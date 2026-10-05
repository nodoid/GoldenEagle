using System;
using System.Collections.Generic;
using System.Linq;
using GoldenEagle.Core.Audio;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;
using GoldenEagle.Core.World;

namespace GoldenEagle.Core.Engine;

public enum Outcome
{
    Playing,
    Won,
    DiedOfExhaustion,
    FellIntoAbyss,
}

public enum PotionEffect
{
    Heal,
    Poison,
}

/// <summary>An object somewhere in the castle (or carried, when <see cref="Room"/> is null).</summary>
public sealed class ItemInstance
{
    public ItemInstance(int id, ItemKind kind, OricColor color, string room, float x, float z)
    {
        Id = id;
        Kind = kind;
        Color = color;
        Room = room;
        X = x;
        Z = z;
    }

    public int Id { get; }
    public ItemKind Kind { get; }
    public OricColor Color { get; }
    public string Room { get; internal set; }
    public float X { get; internal set; }
    public float Z { get; internal set; }

    public Loc Name => ItemInfo.Name(Kind, Color);

    public override string ToString() => $"{Kind}({Color}) in {Room ?? "hand"}";
}

public sealed class Monster
{
    public Monster(MonsterKind kind, string room, float x, float z)
    {
        Kind = kind;
        Room = room;
        HomeX = X = x;
        HomeZ = Z = z;
    }

    public MonsterKind Kind { get; }
    public string Room { get; }
    public float HomeX { get; }
    public float HomeZ { get; }
    public float X { get; internal set; }
    public float Z { get; internal set; }
    public float Vx { get; internal set; }
    public float Vz { get; internal set; }

    /// <summary>Height above the floor, for flying bats.</summary>
    public float Height { get; internal set; }

    public int Cooldown { get; internal set; }
    public bool Fleeing { get; internal set; }

    internal void Reset()
    {
        X = HomeX;
        Z = HomeZ;
        Vx = Kind == MonsterKind.Knight ? Adventure.KnightSpeed : 0;
        Vz = 0;
        Cooldown = 0;
        Fleeing = false;
    }
}

/// <summary>Where the player wants to walk: a floor point, an object to take, or a door to go through.</summary>
public readonly record struct WalkTarget(float X, float Z, ItemInstance Item = null, DoorEnd Door = null);

/// <summary>The player's wishes for one tick.</summary>
public sealed class Command
{
    public float MoveX;
    public float MoveZ;
    public WalkTarget? Walk;
    public bool Take;
    public bool Use;
    public bool Drop;
    public bool NextSlot;
    public int? Slot;

    public void Clear()
    {
        MoveX = MoveZ = 0;
        Walk = null;
        Take = Use = Drop = NextSlot = false;
        Slot = null;
    }
}

/// <summary>
/// One game of the Golden Eagle: the castle's state and every rule. Rooms are boxes seen in
/// perspective; positions are x (-1 left wall .. 1 right wall) and z (0 near .. 1 back wall).
/// Runs at 50 ticks per second and needs no graphics, so tests and the autopilot drive it directly.
/// </summary>
public sealed class Adventure
{
    public const int TicksPerSecond = 50;
    public const int Capacity = 4;
    public const int MaxEnergy = 100;
    public const float XMin = -0.86f, XMax = 0.86f, ZMin = 0.03f, ZMax = 0.95f;
    public const float SpeedX = 0.016f, SpeedZ = 0.012f;
    public const float DoorHalfWidth = 0.17f;
    public const float SideDoorHalfDepth = 0.09f;
    public const float Reach = 0.2f;
    public const float KnightSpeed = 0.007f;
    public const int DrainTicks = 180;
    public const int VisitScore = 25;
    public const int UnlockScore = 50;
    public const int EagleScore = 1000;
    public const int EscapeScore = 2000;

    private readonly ISoundPlayer _sound;
    private readonly Random _random;
    private readonly List<ItemInstance> _items = new();
    private readonly List<ItemInstance> _inventory = new();
    private readonly List<Monster> _monsters = new();
    private readonly HashSet<int> _unlocked = new();
    private readonly HashSet<string> _visited = new();
    private readonly Dictionary<OricColor, PotionEffect> _potions = new();
    private int _drain;
    private int _graceTicks;
    private int _lockedMessageCooldown;
    private WalkTarget? _walk;
    private WalkTarget? _waypoint;

    public Adventure(ISoundPlayer sound = null, int seed = 1984)
    {
        _sound = sound ?? SilentSoundPlayer.Instance;
        _random = new Random(seed);
        Castle = Castle.Build();
        Seed = seed;

        int id = 0;
        foreach (var room in Castle.Rooms)
        {
            foreach (var p in room.Items)
                _items.Add(new ItemInstance(id++, p.Kind, p.Color, room.Id, p.X, p.Z));
            foreach (var m in room.Monsters)
            {
                var monster = new Monster(m.Kind, room.Id, m.X, m.Z);
                monster.Reset();
                _monsters.Add(monster);
            }
        }

        // One potion colour is poison; the others heal. Which one changes every game.
        var colours = _items.Where(i => i.Kind == ItemKind.Potion).Select(i => i.Color).Distinct().ToList();
        int poison = _random.Next(colours.Count);
        for (int i = 0; i < colours.Count; i++)
            _potions[colours[i]] = i == poison ? PotionEffect.Poison : PotionEffect.Heal;

        Energy = MaxEnergy;
        EnterRoom(Castle.Start, 0f, 0.15f, Side.Back);
    }

    public Castle Castle { get; }
    public int Seed { get; }
    public string RoomId { get; private set; }
    public RoomDef Room => Castle[RoomId];
    public float X { get; private set; }
    public float Z { get; private set; }

    /// <summary>The way the hero faces: Back (away from the viewer), Front, Left or Right.</summary>
    public Side Facing { get; private set; } = Side.Back;

    public bool Moving { get; private set; }
    public int Steps { get; private set; }
    public int Energy { get; private set; }
    public int Score { get; private set; }
    public int Ticks { get; private set; }
    public Outcome Outcome { get; private set; }
    public bool Invulnerable { get; set; }
    public int Selected { get; private set; }

    /// <summary>Ticks since the hero last got hurt (for the flashing).</summary>
    public int HurtTicks { get; private set; } = 1000;

    /// <summary>Ticks since entering the current room.</summary>
    public int RoomTicks { get; private set; }

    public Func<string> Message { get; private set; }
    public int MessageTicks { get; private set; }

    public IReadOnlyList<ItemInstance> Inventory => _inventory;
    public IReadOnlyList<ItemInstance> AllItems => _items;
    public IReadOnlyCollection<string> Visited => _visited;
    public WalkTarget? Walk => _walk;

    public ItemInstance SelectedItem => Selected < _inventory.Count ? _inventory[Selected] : null;

    public IEnumerable<ItemInstance> ItemsIn(string room) => _items.Where(i => i.Room == room);

    public IEnumerable<Monster> MonstersIn(string room) => _monsters.Where(m => m.Room == room);

    public bool IsUnlocked(Door door) => door.LockColor == null || _unlocked.Contains(door.Id);

    public bool Has(ItemKind kind) => _inventory.Any(i => i.Kind == kind);

    public bool HasKey(OricColor color) => _inventory.Any(i => i.Kind == ItemKind.Key && i.Color == color);

    public PotionEffect PotionEffectOf(OricColor color) => _potions[color];

    /// <summary>True when the hero can see the room's contents: lit rooms, or dark ones with the lamp.</summary>
    public bool CanSee(string room) => !Castle[room].Dark || Has(ItemKind.Lamp);

    public int Seconds => Ticks / TicksPerSecond;

    // ---------------------------------------------------------------- tick

    public void Tick(Command cmd)
    {
        if (Outcome != Outcome.Playing)
            return;
        Ticks++;
        RoomTicks++;
        HurtTicks++;
        if (MessageTicks > 0)
            MessageTicks--;
        if (_lockedMessageCooldown > 0)
            _lockedMessageCooldown--;
        if (_graceTicks > 0)
            _graceTicks--;

        if (cmd != null)
        {
            if (cmd.Slot is int slot && slot >= 0 && slot < Capacity)
                Selected = slot;
            if (cmd.NextSlot)
                Selected = (Selected + 1) % Capacity;
            if (cmd.Walk is WalkTarget w)
            {
                _walk = w;
                _waypoint = null;
            }
            if (cmd.Take)
                Take(null);
            if (cmd.Use)
                UseSelected();
            if (cmd.Drop)
                DropSelected();
        }

        Move(cmd);
        if (Outcome != Outcome.Playing)
            return;
        CheckPits();
        if (Outcome != Outcome.Playing)
            return;
        UpdateMonsters();
        Drain();
    }

    private void Drain()
    {
        if (++_drain < DrainTicks)
            return;
        _drain = 0;
        if (Invulnerable)
            return;
        Energy--;
        if (Energy == 20)
            Say(() => Strings.Tired, Sfx.Warning);
        if (Energy <= 0)
            Die(Outcome.DiedOfExhaustion);
    }

    private void Hurt(int amount, Func<string> message, Sfx sfx)
    {
        Say(message, sfx);
        HurtTicks = 0;
        if (Invulnerable)
            return;
        Energy = Math.Max(0, Energy - amount);
        if (Energy <= 0)
            Die(Outcome.DiedOfExhaustion);
    }

    private void Die(Outcome how)
    {
        Outcome = how;
        _walk = null;
        _sound.Play(Sfx.Death);
    }

    public void Say(Func<string> message, Sfx? sfx = null)
    {
        Message = message;
        MessageTicks = 150;
        if (sfx is Sfx s)
            _sound.Play(s);
    }

    // ---------------------------------------------------------------- movement

    private void Move(Command cmd)
    {
        float wantX = cmd?.MoveX ?? 0, wantZ = cmd?.MoveZ ?? 0;
        if (wantX != 0 || wantZ != 0)
        {
            _walk = null;
            _waypoint = null;
        }

        float dx, dz;
        if (_walk is WalkTarget target)
        {
            var aim = Aim(target);
            float ex = aim.x - X, ez = aim.z - Z;
            dx = Math.Clamp(ex, -SpeedX, SpeedX);
            dz = Math.Clamp(ez, -SpeedZ, SpeedZ);
            if (target.Item != null && target.Item.Room == RoomId && Near(target.Item))
            {
                _walk = null;
                Take(target.Item);
                dx = dz = 0;
            }
            else if (target.Item != null && target.Item.Room != RoomId)
            {
                _walk = null;
            }
            else if (target.Door == null && _waypoint == null && Math.Abs(ex) < 0.001f && Math.Abs(ez) < 0.001f)
            {
                _walk = null;
            }
            else if (_waypoint is WalkTarget wp && Math.Abs(wp.X - X) < 0.02f && Math.Abs(wp.Z - Z) < 0.02f)
            {
                _waypoint = null;
            }
        }
        else
        {
            dx = wantX * SpeedX;
            dz = wantZ * SpeedZ;
        }

        Moving = Math.Abs(dx) > 0.0005f || Math.Abs(dz) > 0.0005f;
        if (!Moving)
            return;

        if (Math.Abs(dx) * 0.75f > Math.Abs(dz))
            Facing = dx < 0 ? Side.Left : Side.Right;
        else
            Facing = dz > 0 ? Side.Back : Side.Front;

        float nx = X + dx, nz = Z + dz;
        if (TryExit(nx, nz))
            return;
        X = Math.Clamp(nx, XMin, XMax);
        Z = Math.Clamp(nz, ZMin, ZMax);
        Steps++;
        if (Steps % 14 == 0)
            _sound.Play(Sfx.Step);
    }

    /// <summary>The point to head for now: the target, or a waypoint that skirts a visible pit.</summary>
    private (float x, float z) Aim(WalkTarget target)
    {
        float tx = target.X, tz = target.Z;
        if (target.Door != null)
            (tx, tz) = DoorApproach(target.Door, outside: true);
        else if (target.Item != null)
            (tx, tz) = (target.Item.X, target.Item.Z);
        tx = target.Door != null ? tx : Math.Clamp(tx, XMin, XMax);
        tz = target.Door != null ? tz : Math.Clamp(tz, ZMin, ZMax);

        if (_waypoint is WalkTarget w)
            return (w.X, w.Z);
        if (CanSee(RoomId))
        {
            var detour = Pathing.Detour(X, Z, Math.Clamp(tx, XMin, XMax), Math.Clamp(tz, ZMin, ZMax), Room.Pits);
            if (detour is (float wx, float wz))
            {
                _waypoint = new WalkTarget(wx, wz);
                return (wx, wz);
            }
        }
        return (tx, tz);
    }

    /// <summary>A point by a door: just inside it, or just beyond the wall (to walk through it).</summary>
    public static (float x, float z) DoorApproach(DoorEnd end, bool outside)
    {
        float o = outside ? 0.08f : -0.06f;
        return end.Side switch
        {
            Side.Back => (end.Position, ZMax + o),
            Side.Front => (end.Position, ZMin - o),
            Side.Left => (XMin - o, end.Position),
            _ => (XMax + o, end.Position),
        };
    }

    private bool InDoorway(DoorEnd end, float nx, float nz) => end.Side switch
    {
        Side.Back => nz > ZMax && Math.Abs(nx - end.Position) <= DoorHalfWidth,
        Side.Front => nz < ZMin && Math.Abs(nx - end.Position) <= DoorHalfWidth,
        Side.Left => nx < XMin && Math.Abs(nz - end.Position) <= SideDoorHalfDepth,
        _ => nx > XMax && Math.Abs(nz - end.Position) <= SideDoorHalfDepth,
    };

    private bool TryExit(float nx, float nz)
    {
        foreach (var door in Castle.DoorsOf(RoomId))
        {
            var end = door.EndIn(RoomId);
            if (!InDoorway(end, nx, nz))
                continue;

            if (end.Kind == ExitKind.Gate)
            {
                if (Has(ItemKind.Eagle))
                {
                    Win();
                    return true;
                }
                if (_lockedMessageCooldown == 0)
                {
                    Say(() => Strings.NeedEagle, Sfx.Locked);
                    _lockedMessageCooldown = 100;
                }
                _walk = null;
                return false;
            }

            if (!IsUnlocked(door))
            {
                var color = door.LockColor!.Value;
                var key = _inventory.FirstOrDefault(i => i.Kind == ItemKind.Key && i.Color == color);
                if (key == null)
                {
                    if (_lockedMessageCooldown == 0)
                    {
                        var keyName = ItemInfo.Name(ItemKind.Key, color);
                        Say(() => Strings.DoorLocked(keyName.ToString()), Sfx.Locked);
                        _lockedMessageCooldown = 100;
                    }
                    _walk = null;
                    return false;
                }
                // The key stays in the lock.
                _inventory.Remove(key);
                _items.Remove(key);
                ClampSelection();
                _unlocked.Add(door.Id);
                Score += UnlockScore;
                Say(() => Strings.DoorUnlocked, Sfx.Unlock);
            }

            var other = door.Other(RoomId);
            var (x, z) = DoorApproach(other, outside: false);
            Side facing = other.Side switch
            {
                Side.Back => Side.Front,
                Side.Front => Side.Back,
                Side.Left => Side.Right,
                _ => Side.Left,
            };
            _sound.Play(end.Kind == ExitKind.Door ? Sfx.Door : Sfx.Stairs);
            EnterRoom(other.Room, Math.Clamp(x, XMin, XMax), Math.Clamp(z, ZMin, ZMax), facing);
            return true;
        }
        return false;
    }

    private void EnterRoom(string room, float x, float z, Side facing)
    {
        RoomId = room;
        X = x;
        Z = z;
        Facing = facing;
        RoomTicks = 0;
        _walk = null;
        _waypoint = null;
        _graceTicks = 30;
        foreach (var m in MonstersIn(room))
            m.Reset();
        if (_visited.Add(room) && room != Castle.Start)
            Score += VisitScore;
        if (Castle[room].Dark && !Has(ItemKind.Lamp))
            Say(() => Strings.ItIsDark);
        else
            MessageTicks = Math.Min(MessageTicks, 60);
    }

    private void CheckPits()
    {
        foreach (var pit in Room.Pits)
        {
            if (!pit.Contains(X, Z))
                continue;
            _sound.Play(Sfx.Fall);
            if (pit.Below == null)
            {
                Die(Outcome.FellIntoAbyss);
                return;
            }
            EnterRoom(pit.Below, Math.Clamp(X, -0.6f, 0.6f), 0.5f, Side.Front);
            Hurt(10, () => Strings.YouFall, Sfx.Hurt);
            return;
        }
    }

    private void Win()
    {
        Outcome = Outcome.Won;
        _walk = null;
        Score += EscapeScore + Energy * 10 + Math.Max(0, 3000 - Seconds * 5);
        _sound.Play(Sfx.Victory);
    }

    // ---------------------------------------------------------------- objects

    /// <summary>Distance used for reaching things: depth counts double, as it looks shorter.</summary>
    public static float Distance(float x0, float z0, float x1, float z1)
    {
        float dx = x1 - x0, dz = (z1 - z0) * 2f;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    public bool Near(ItemInstance item) => Distance(X, Z, item.X, item.Z) <= Reach;

    public ItemInstance NearestItem() =>
        CanSee(RoomId)
            ? ItemsIn(RoomId).Where(Near).OrderBy(i => Distance(X, Z, i.X, i.Z)).FirstOrDefault()
            : null;

    /// <summary>Picks up <paramref name="item"/>, or the nearest object in reach when null.</summary>
    public bool Take(ItemInstance item)
    {
        if (!CanSee(RoomId))
        {
            Say(() => Strings.ItIsDark);
            return false;
        }
        item ??= NearestItem();
        if (item == null || item.Room != RoomId || !Near(item))
        {
            Say(() => Strings.NothingHere);
            return false;
        }

        if (ItemInfo.IsTreasure(item.Kind))
        {
            int points = ItemInfo.TreasureValue(item.Kind);
            Score += points;
            _items.Remove(item);
            item.Room = null;
            var name = item.Name;
            Say(() => Strings.Treasure(name.ToString(), points), Sfx.Treasure);
            return true;
        }

        if (_inventory.Count >= Capacity)
        {
            Say(() => Strings.HandsFull, Sfx.Locked);
            return false;
        }

        item.Room = null;
        _inventory.Add(item);
        Selected = _inventory.Count - 1;
        var taken = item.Name;
        if (item.Kind == ItemKind.Eagle)
        {
            Score += EagleScore;
            Say(() => Strings.YouTake(taken.ToString()) + " " + Strings.EagleHint, Sfx.Eagle);
            Message = () => Strings.EagleHint;
        }
        else
        {
            Say(() => Strings.YouTake(taken.ToString()), Sfx.Take);
        }
        return true;
    }

    public bool DropSelected()
    {
        var item = SelectedItem;
        if (item == null)
        {
            Say(() => Strings.NothingToUse);
            return false;
        }
        _inventory.Remove(item);
        item.Room = RoomId;
        item.X = X;
        item.Z = Math.Max(ZMin, Z - 0.02f);
        ClampSelection();
        var name = item.Name;
        Say(() => Strings.YouDrop(name.ToString()), Sfx.Drop);
        return true;
    }

    /// <summary>Selects the inventory slot holding <paramref name="item"/>.</summary>
    public void Select(ItemInstance item)
    {
        int i = _inventory.IndexOf(item);
        if (i >= 0)
            Selected = i;
    }

    public bool UseSelected()
    {
        var item = SelectedItem;
        if (item == null)
        {
            Say(() => Strings.NothingToUse);
            return false;
        }
        switch (item.Kind)
        {
            case ItemKind.Potion:
                Consume(item);
                if (_potions[item.Color] == PotionEffect.Heal)
                {
                    Energy = Math.Min(MaxEnergy, Energy + 40);
                    Say(() => Strings.Healed, Sfx.Drink);
                }
                else
                {
                    Hurt(30, () => Strings.Poisoned, Sfx.Poison);
                }
                return true;
            case ItemKind.Bread:
                Consume(item);
                Energy = Math.Min(MaxEnergy, Energy + 30);
                Say(() => Strings.Eaten, Sfx.Eat);
                return true;
            case ItemKind.Lamp:
                Say(() => Strings.LampLit, Sfx.Take);
                return true;
            case ItemKind.Cross:
                Say(() => Strings.CrossRaised, Sfx.Take);
                return true;
            case ItemKind.Key:
                Say(() => Strings.KeyHint);
                return true;
            default:
                Say(() => Strings.EagleHint, Sfx.Eagle);
                return true;
        }
    }

    private void Consume(ItemInstance item)
    {
        _inventory.Remove(item);
        _items.Remove(item);
        item.Room = null;
        ClampSelection();
    }

    private void ClampSelection()
    {
        if (Selected >= _inventory.Count)
            Selected = Math.Max(0, _inventory.Count - 1);
    }

    // ---------------------------------------------------------------- monsters

    private void UpdateMonsters()
    {
        bool cross = Has(ItemKind.Cross);
        foreach (var m in MonstersIn(RoomId))
        {
            if (m.Cooldown > 0)
                m.Cooldown--;
            switch (m.Kind)
            {
                case MonsterKind.Ghost:
                    UpdateGhost(m, cross);
                    break;
                case MonsterKind.Bat:
                    UpdateBat(m);
                    break;
                default:
                    UpdateKnight(m);
                    break;
            }
            if (_graceTicks > 0 || m.Cooldown > 0 || Outcome != Outcome.Playing)
                continue;
            if (Math.Abs(m.X - X) < 0.13f && Math.Abs(m.Z - Z) < 0.07f)
                Touch(m);
        }
    }

    private void UpdateGhost(Monster m, bool cross)
    {
        m.Fleeing = cross;
        float speed = cross ? 0.008f : 0.0045f;
        float sign = cross ? -1f : 1f;
        float dx = X - m.X, dz = Z - m.Z;
        float len = MathF.Max(0.001f, MathF.Sqrt(dx * dx + dz * dz));
        m.X = Math.Clamp(m.X + sign * dx / len * speed, XMin, XMax);
        m.Z = Math.Clamp(m.Z + sign * dz / len * speed * 0.7f, 0.15f, ZMax);
        m.Height = 0.12f + 0.05f * MathF.Sin(Ticks * 0.08f);
    }

    private void UpdateBat(Monster m)
    {
        if (Ticks % 30 == 0 || (m.Vx == 0 && m.Vz == 0))
        {
            // Flutters about, drifting towards the hero.
            float toward = MathF.Sign(X - m.X) * 0.004f;
            m.Vx = (float)(_random.NextDouble() * 0.024 - 0.012) + toward;
            m.Vz = (float)(_random.NextDouble() * 0.016 - 0.008) + MathF.Sign(Z - m.Z) * 0.003f;
        }
        m.X += m.Vx;
        m.Z += m.Vz;
        if (m.X < XMin || m.X > XMax) m.Vx = -m.Vx;
        if (m.Z < 0.15f || m.Z > ZMax) m.Vz = -m.Vz;
        m.X = Math.Clamp(m.X, XMin, XMax);
        m.Z = Math.Clamp(m.Z, 0.15f, ZMax);
        m.Height = 0.45f + 0.08f * MathF.Sin(Ticks * 0.3f);
    }

    private void UpdateKnight(Monster m)
    {
        m.X += m.Vx;
        if (m.X < -0.7f || m.X > 0.7f)
        {
            m.Vx = -m.Vx;
            m.X = Math.Clamp(m.X, -0.7f, 0.7f);
        }
        m.Height = 0;
    }

    private void Touch(Monster m)
    {
        switch (m.Kind)
        {
            case MonsterKind.Ghost:
                if (m.Fleeing)
                    return;
                m.Cooldown = 25;
                Hurt(3, () => Strings.GhostTouch, Sfx.Ghost);
                break;
            case MonsterKind.Bat:
                m.Cooldown = 80;
                m.Vx = -m.Vx;
                m.Vz = Math.Abs(m.Vz) + 0.006f;
                Hurt(4, () => Strings.BatBite, Sfx.Hurt);
                break;
            default:
                m.Cooldown = 60;
                Z = Math.Clamp(Z + (Z < m.Z ? -0.16f : 0.16f), ZMin, ZMax);
                _walk = _walk is { Door: not null } or { Item: not null } ? _walk : null;
                _waypoint = null;
                Hurt(6, () => Strings.KnightHit, Sfx.Hurt);
                break;
        }
    }

    // ---------------------------------------------------------------- testing / demo helpers

    /// <summary>Moves the hero (demo scenes and tests).</summary>
    internal void Teleport(string room, float x, float z)
    {
        EnterRoom(room, x, z, Side.Back);
    }

    internal void Give(ItemKind kind, OricColor color = OricColor.Yellow)
    {
        var item = _items.FirstOrDefault(i => i.Kind == kind && i.Color == color && i.Room != null);
        if (item == null)
            return;
        item.Room = null;
        _inventory.Add(item);
    }

    internal void SetEnergy(int energy) => Energy = energy;
}
