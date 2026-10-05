using System.Collections.Generic;
using System.Linq;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.World;

namespace GoldenEagle.Tests;

public class CastleTests
{
    private readonly Castle _castle = Castle.Build();

    [Fact]
    public void Every_door_joins_rooms_that_exist()
    {
        foreach (var d in _castle.Doors)
        {
            Assert.True(_castle.Has(d.A.Room), d.A.Room);
            Assert.True(_castle.Has(d.B.Room), d.B.Room);
        }
    }

    [Fact]
    public void Every_room_can_be_reached_from_the_gatehouse()
    {
        var seen = new HashSet<string> { Castle.Start };
        var queue = new Queue<string>(seen);
        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            foreach (var d in _castle.DoorsOf(room))
            {
                var next = d.Other(room).Room;
                if (seen.Add(next))
                    queue.Enqueue(next);
            }
        }
        Assert.Equal(_castle.Rooms.Select(r => r.Id).OrderBy(x => x), seen.OrderBy(x => x));
    }

    [Fact]
    public void There_is_exactly_one_gate_and_it_is_in_the_gatehouse()
    {
        var gates = _castle.Doors.Where(d => d.A.Kind == ExitKind.Gate).ToList();
        Assert.Single(gates);
        Assert.Equal(Castle.Start, gates[0].A.Room);
        Assert.Equal(Side.Front, gates[0].A.Side);
    }

    [Fact]
    public void Stairs_go_up_one_way_and_down_the_other()
    {
        foreach (var d in _castle.Doors.Where(d => d.A.Kind is ExitKind.StairsUp or ExitKind.StairsDown))
        {
            Assert.NotEqual(d.A.Kind, d.B.Kind);
            var up = d.A.Kind == ExitKind.StairsUp ? d.A : d.B;
            var down = d.A.Kind == ExitKind.StairsUp ? d.B : d.A;
            Assert.True(_castle[up.Room].Floor < _castle[down.Room].Floor,
                $"stairs up from {up.Room} should lead to a higher floor than {down.Room}");
        }
    }

    [Fact]
    public void Doors_on_the_same_wall_do_not_overlap()
    {
        foreach (var room in _castle.Rooms)
        {
            var ends = _castle.DoorsOf(room.Id).Select(d => d.EndIn(room.Id)).ToList();
            foreach (var group in ends.GroupBy(e => e.Side))
            {
                var list = group.OrderBy(e => e.Position).ToList();
                float gap = group.Key is Side.Back or Side.Front ? Adventure.DoorHalfWidth * 2 : Adventure.SideDoorHalfDepth * 2;
                for (int i = 1; i < list.Count; i++)
                    Assert.True(list[i].Position - list[i - 1].Position > gap, $"{room.Id} {group.Key}");
            }
        }
    }

    [Fact]
    public void Door_positions_lie_on_their_walls()
    {
        foreach (var end in _castle.Doors.SelectMany(d => new[] { d.A, d.B }))
        {
            if (end.Side is Side.Back or Side.Front)
                Assert.InRange(end.Position, Adventure.XMin + 0.05f, Adventure.XMax - 0.05f);
            else
                Assert.InRange(end.Position, 0.15f, 0.85f);
        }
    }

    [Fact]
    public void Every_locked_door_has_exactly_one_key()
    {
        var keys = _castle.Rooms.SelectMany(r => r.Items).Where(i => i.Kind == ItemKind.Key).ToList();
        foreach (var door in _castle.Doors.Where(d => d.LockColor != null))
            Assert.Single(keys, k => k.Color == door.LockColor);
        Assert.Equal(keys.Count, _castle.Doors.Count(d => d.LockColor != null));
    }

    [Fact]
    public void There_is_one_golden_eagle_and_it_is_in_the_eyrie()
    {
        var eagles = _castle.Rooms.SelectMany(r => r.Items.Select(i => (r.Id, i))).Where(p => p.i.Kind == ItemKind.Eagle).ToList();
        Assert.Single(eagles);
        Assert.Equal(Castle.Eyrie, eagles[0].Id);
    }

    [Fact]
    public void Things_in_rooms_are_inside_the_walls_and_not_over_trapdoors()
    {
        foreach (var room in _castle.Rooms)
        {
            foreach (var i in room.Items)
            {
                Assert.InRange(i.X, Adventure.XMin, Adventure.XMax);
                Assert.InRange(i.Z, Adventure.ZMin, Adventure.ZMax);
                Assert.DoesNotContain(room.Pits, p => p.Contains(i.X, i.Z));
            }
            foreach (var m in room.Monsters)
            {
                Assert.InRange(m.X, Adventure.XMin, Adventure.XMax);
                Assert.InRange(m.Z, Adventure.ZMin, Adventure.ZMax);
            }
        }
    }

    [Fact]
    public void Trapdoors_drop_into_rooms_on_the_floor_below()
    {
        foreach (var room in _castle.Rooms)
            foreach (var pit in room.Pits.Where(p => p.Below != null))
                Assert.True(_castle[pit.Below].Floor < room.Floor, $"{room.Id} -> {pit.Below}");
    }

    [Fact]
    public void Trapdoors_keep_clear_of_doorways()
    {
        foreach (var room in _castle.Rooms)
        {
            foreach (var end in _castle.DoorsOf(room.Id).Select(d => d.EndIn(room.Id)))
            {
                var (x, z) = Adventure.DoorApproach(end, outside: false);
                Assert.DoesNotContain(room.Pits, p => p.Contains(x, z));
            }
        }
    }

    [Fact]
    public void Every_room_has_a_name_in_both_languages()
    {
        foreach (var room in _castle.Rooms)
        {
            Assert.False(string.IsNullOrWhiteSpace(room.Name.Fr));
            Assert.False(string.IsNullOrWhiteSpace(room.Name.En));
            Assert.True(room.Name.Fr.Length <= 40 && room.Name.En.Length <= 40);
        }
        Assert.Equal(_castle.Rooms.Count, _castle.Rooms.Select(r => r.Name.En).Distinct().Count());
        Assert.Equal(_castle.Rooms.Count, _castle.Rooms.Select(r => r.Name.Fr).Distinct().Count());
    }

    [Fact]
    public void The_castle_has_cellars_ground_upper_floor_and_tower()
    {
        Assert.Equal(4, _castle.Rooms.Select(r => r.Floor).Distinct().Count());
        Assert.Contains(_castle.Rooms, r => r.Dark);
    }
}
