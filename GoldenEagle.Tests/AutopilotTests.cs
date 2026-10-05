using System.Linq;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.World;

namespace GoldenEagle.Tests;

/// <summary>Proves the castle can be completed under the normal rules.</summary>
public class AutopilotTests
{
    private static Adventure Play(int seed, bool treasure, int limit = 60000)
    {
        var a = new Adventure(seed: seed);
        var pilot = new Autopilot(a) { CollectTreasure = treasure };
        for (int i = 0; i < limit && a.Outcome == Outcome.Playing; i++)
            a.Tick(pilot.Next());
        return a;
    }

    [Theory]
    [InlineData(1984)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2026)]
    public void The_autopilot_brings_the_eagle_home(int seed)
    {
        var a = Play(seed, treasure: true);
        Assert.Equal(Outcome.Won, a.Outcome);
        Assert.True(a.Score > Adventure.EagleScore + Adventure.EscapeScore);
    }

    [Fact]
    public void The_shortest_route_wins_without_treasure()
    {
        var a = Play(5, treasure: false);
        Assert.Equal(Outcome.Won, a.Outcome);
    }

    [Fact]
    public void Every_coloured_door_is_opened_on_the_way()
    {
        var a = Play(1984, treasure: false);
        Assert.All(a.Castle.Doors.Where(d => d.LockColor != null), d => Assert.True(a.IsUnlocked(d)));
    }

    [Fact]
    public void The_eagle_cannot_be_reached_without_the_keys()
    {
        var a = new Adventure();
        var pilot = new Autopilot(a);
        Assert.False(pilot.Distances().ContainsKey(Castle.Eyrie));
        Assert.Null(pilot.NextDoor(Castle.Eyrie));
    }

    [Fact]
    public void Treasure_hunting_finds_more_treasure()
    {
        static int Left(Adventure a) => a.AllItems.Count(i => ItemInfo.IsTreasure(i.Kind));
        Assert.True(Left(Play(3, treasure: true)) < Left(Play(3, treasure: false)));
    }
}
