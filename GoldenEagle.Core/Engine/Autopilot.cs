using System.Collections.Generic;
using System.Linq;
using GoldenEagle.Core.World;

namespace GoldenEagle.Core.Engine;

/// <summary>
/// Plays the game by itself: fetches whatever is useful and reachable (keys for locked doors,
/// the lamp, the cross, treasure, bread when weak, then the eagle) and carries the eagle out of
/// the gate. Used by the attract mode, the store captures and the tests that prove the castle
/// can be completed.
/// </summary>
public sealed class Autopilot
{
    private readonly Adventure _a;
    private readonly Command _cmd = new();
    private ItemInstance _goal;

    public Autopilot(Adventure adventure)
    {
        _a = adventure;
    }

    /// <summary>When false, treasure is ignored and the pilot goes straight for what it needs.</summary>
    public bool CollectTreasure { get; set; } = true;

    public ItemInstance Goal => _goal;

    public Command Next()
    {
        _cmd.Clear();
        if (_a.Outcome != Outcome.Playing)
            return _cmd;

        if (_a.Energy < 55 && Eat())
            return _cmd;

        if (_a.Has(ItemKind.Eagle))
        {
            _goal = null;
            if (_a.RoomId == Castle.Start)
                WalkThrough(_a.Castle.Gate.A);
            else
                HeadFor(Castle.Start);
            return _cmd;
        }

        if (_goal == null || _goal.Room == null || !Wanted(_goal) || !Distances().ContainsKey(_goal.Room))
            _goal = ChooseGoal();
        if (_goal == null)
            return _cmd;

        if (_a.RoomId != _goal.Room)
        {
            HeadFor(_goal.Room);
            return _cmd;
        }

        if (!ItemInfo.IsTreasure(_goal.Kind) && _a.Inventory.Count >= Adventure.Capacity)
        {
            MakeRoom();
            return _cmd;
        }

        if (_a.Walk is not { Item: not null } w || w.Item != _goal)
            _cmd.Walk = new WalkTarget(_goal.X, _goal.Z, _goal);
        return _cmd;
    }

    private bool Eat()
    {
        var bread = _a.Inventory.FirstOrDefault(i => i.Kind == ItemKind.Bread);
        if (bread == null)
            return false;
        _a.Select(bread);
        _cmd.Use = true;
        return true;
    }

    private void MakeRoom()
    {
        ItemKind[] order = [ItemKind.Potion, ItemKind.Bread, ItemKind.Cross, ItemKind.Lamp];
        foreach (var kind in order)
        {
            var item = _a.Inventory.FirstOrDefault(i => i.Kind == kind);
            if (item == null)
                continue;
            _a.Select(item);
            _cmd.Drop = true;
            return;
        }
    }

    private bool Wanted(ItemInstance item)
    {
        if (item.Room == null || !_a.CanSee(item.Room))
            return false;
        return item.Kind switch
        {
            ItemKind.Eagle => true,
            ItemKind.Key => _a.Castle.Doors.Any(d => d.LockColor == item.Color && !_a.IsUnlocked(d)),
            ItemKind.Lamp => !_a.Has(ItemKind.Lamp),
            ItemKind.Cross => !_a.Has(ItemKind.Cross),
            ItemKind.Bread => _a.Energy <= 70 && !_a.Has(ItemKind.Bread),
            ItemKind.Potion => false,
            _ => CollectTreasure,
        };
    }

    private static int Priority(ItemKind kind) => kind switch
    {
        ItemKind.Eagle => 0,
        ItemKind.Bread => 1,
        ItemKind.Key => 2,
        ItemKind.Lamp or ItemKind.Cross => 3,
        _ => 4,
    };

    private ItemInstance ChooseGoal()
    {
        var dist = Distances();
        return _a.AllItems
            .Where(i => i.Room != null && dist.ContainsKey(i.Room) && Wanted(i))
            .OrderBy(i => dist[i.Room] * 10 + Priority(i.Kind))
            .ThenBy(i => Adventure.Distance(_a.X, _a.Z, i.X, i.Z))
            .FirstOrDefault();
    }

    private bool Passable(Door door) =>
        door.A.Kind != ExitKind.Gate && (_a.IsUnlocked(door) || _a.HasKey(door.LockColor!.Value));

    /// <summary>Rooms reachable from here through doors the hero can open, with the number of doors to each.</summary>
    public Dictionary<string, int> Distances()
    {
        var dist = new Dictionary<string, int> { [_a.RoomId] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(_a.RoomId);
        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            foreach (var door in _a.Castle.DoorsOf(room).Where(Passable))
            {
                var next = door.Other(room).Room;
                if (dist.ContainsKey(next))
                    continue;
                dist[next] = dist[room] + 1;
                queue.Enqueue(next);
            }
        }
        return dist;
    }

    /// <summary>The door to take from the current room on the shortest way to <paramref name="target"/>.</summary>
    public DoorEnd NextDoor(string target)
    {
        var first = new Dictionary<string, DoorEnd> { [_a.RoomId] = null };
        var queue = new Queue<string>();
        queue.Enqueue(_a.RoomId);
        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            if (room == target)
                return first[room];
            foreach (var door in _a.Castle.DoorsOf(room).Where(Passable))
            {
                var next = door.Other(room).Room;
                if (first.ContainsKey(next))
                    continue;
                first[next] = first[room] ?? door.EndIn(room);
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private void HeadFor(string room)
    {
        var door = NextDoor(room);
        if (door != null)
            WalkThrough(door);
    }

    private void WalkThrough(DoorEnd end)
    {
        if (_a.Walk is { Door: not null } w && w.Door == end)
            return;
        var (x, z) = Adventure.DoorApproach(end, outside: true);
        _cmd.Walk = new WalkTarget(x, z, Door: end);
    }
}
