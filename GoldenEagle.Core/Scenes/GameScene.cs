using System.Linq;
using Microsoft.Xna.Framework;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Graphics;

namespace GoldenEagle.Core.Scenes;

/// <summary>Playing the game: turns keys, clicks and taps into the hero's commands.</summary>
public sealed class GameScene : Scene, IPausable
{
    private readonly Adventure _adventure;
    private readonly Command _cmd = new();
    private int _overTicks;

    public GameScene(GoldenEagleGame game, Adventure adventure) : base(game)
    {
        _adventure = adventure;
    }

    public Adventure Adventure => _adventure;

    protected override void Update()
    {
        var input = Game.Input;
        if (input.BackPressed)
        {
            Game.ShowMenu();
            return;
        }
        if (input.LanguagePressed)
            Game.ToggleLanguage();
        if (input.VolumePressed)
            Game.CycleVolume();

        if (_adventure.Outcome != Outcome.Playing)
        {
            if (++_overTicks > 120 || (_overTicks > 40 && (input.ConfirmPressed || input.Taps.Count > 0)))
                Game.GameEnded(_adventure);
            return;
        }

        _cmd.Clear();
        _cmd.MoveX = input.MoveX;
        _cmd.MoveZ = input.MoveZ;
        _cmd.Take = input.TakePressed;
        _cmd.Use = input.UsePressed;
        _cmd.Drop = input.DropPressed;
        _cmd.NextSlot = input.NextSlotPressed;
        _cmd.Slot = input.SlotPressed;

        foreach (var tap in input.Taps)
            Tap(tap);
        if (input.Taps.Count == 0 && input.Holding is Vector2 hold && hold.Y < Perspective.ViewHeight &&
            _adventure.Walk is { Item: null, Door: null } && Perspective.ToFloor(hold.X, hold.Y) is (float fx, float fz))
        {
            // Dragging a finger (or the mouse) steers the walk.
            _cmd.Walk = new WalkTarget(fx, fz);
        }

        _adventure.Tick(_cmd);
    }

    /// <summary>A tap or click: on the panel it works the inventory, in the room it walks, takes or leaves.</summary>
    private void Tap(Vector2 p)
    {
        if (p.Y >= Hud.Top)
        {
            for (int i = 0; i < Engine.Adventure.Capacity; i++)
            {
                if (!Hud.Hit(Hud.Slot(i), p))
                    continue;
                if (i == _adventure.Selected && i < _adventure.Inventory.Count)
                    _cmd.Use = true;
                else
                    _cmd.Slot = i;
                return;
            }
            if (Hud.Hit(Hud.UseButton, p))
                _cmd.Use = true;
            else if (Hud.Hit(Hud.DropButton, p))
                _cmd.Drop = true;
            return;
        }

        var room = _adventure.RoomId;
        if (_adventure.CanSee(room))
        {
            var item = _adventure.ItemsIn(room)
                .Where(i => Inflate(RoomRenderer.ItemRect(i), 4).Contains((int)p.X, (int)p.Y))
                .OrderBy(i => i.Z)
                .FirstOrDefault();
            if (item != null)
            {
                _cmd.Walk = new WalkTarget(item.X, item.Z, item);
                return;
            }
        }

        var floor = Perspective.ToFloor(p.X, p.Y);
        foreach (var door in _adventure.Castle.DoorsOf(room))
        {
            var end = door.EndIn(room);
            if (!RoomRenderer.DoorBounds(end).Contains((int)p.X, (int)p.Y))
                continue;
            // A tap on the floor well inside the room is a walk, even near a front exit.
            if (end.Side == World.Side.Front && floor is (_, float z) && z > 0.12f)
                continue;
            var (x, zz) = Engine.Adventure.DoorApproach(end, outside: true);
            _cmd.Walk = new WalkTarget(x, zz, Door: end);
            return;
        }

        if (floor is (float fx, float fz))
            _cmd.Walk = new WalkTarget(fx, fz);
    }

    private static Rectangle Inflate(Rectangle r, int by) => new(r.X - by, r.Y - by, r.Width + by * 2, r.Height + by * 2);

    public override void Draw(Canvas c)
    {
        RoomRenderer.Draw(c, _adventure, Ticks, Game.IsMobile);
    }
}
