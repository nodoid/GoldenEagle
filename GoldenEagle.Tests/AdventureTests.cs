using System.Linq;
using GoldenEagle.Core.Audio;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Oric;
using GoldenEagle.Core.World;

namespace GoldenEagle.Tests;

public class AdventureTests
{
    private static void Run(Adventure a, Command c, int ticks)
    {
        for (int i = 0; i < ticks && a.Outcome == Outcome.Playing; i++)
            a.Tick(c);
    }

    private static Command Move(float x, float z) => new() { MoveX = x, MoveZ = z };

    private static void GoThrough(Adventure a, DoorEnd end, int limit = 2000)
    {
        string from = a.RoomId;
        var (x, z) = Adventure.DoorApproach(end, outside: true);
        a.Tick(new Command { Walk = new WalkTarget(x, z, Door: end) });
        for (int i = 0; i < limit && a.RoomId == from && a.Outcome == Outcome.Playing; i++)
            a.Tick(new Command());
    }

    private static DoorEnd Exit(Adventure a, string to) =>
        a.Castle.DoorsOf(a.RoomId).First(d => d.Other(a.RoomId).Room == to).EndIn(a.RoomId);

    [Fact]
    public void A_new_game_starts_in_the_gatehouse_with_full_energy()
    {
        var a = new Adventure();
        Assert.Equal(Castle.Start, a.RoomId);
        Assert.Equal(Adventure.MaxEnergy, a.Energy);
        Assert.Empty(a.Inventory);
        Assert.Equal(0, a.Score);
        Assert.Equal(Outcome.Playing, a.Outcome);
    }

    [Fact]
    public void Walking_moves_the_hero_and_walls_stop_him()
    {
        var a = new Adventure();
        float x = a.X;
        Run(a, Move(-1, 0), 10);
        Assert.True(a.X < x);
        Assert.Equal(Side.Left, a.Facing);
        Run(a, Move(-1, 0), 500);
        Assert.Equal(Adventure.XMin, a.X, 3);
        Assert.Equal(Castle.Start, a.RoomId);
    }

    [Fact]
    public void Walking_through_a_door_and_back_returns_to_the_same_doorway()
    {
        var a = new Adventure();
        GoThrough(a, Exit(a, "greathall"));
        Assert.Equal("greathall", a.RoomId);
        Assert.Equal(Side.Back, a.Facing);
        Assert.InRange(a.Z, Adventure.ZMin, 0.1f);
        GoThrough(a, Exit(a, Castle.Start));
        Assert.Equal(Castle.Start, a.RoomId);
        Assert.InRange(a.Z, 0.85f, Adventure.ZMax);
    }

    [Fact]
    public void Visiting_a_new_room_scores_once()
    {
        var a = new Adventure();
        GoThrough(a, Exit(a, "greathall"));
        Assert.Equal(Adventure.VisitScore, a.Score);
        GoThrough(a, Exit(a, Castle.Start));
        GoThrough(a, Exit(a, "greathall"));
        Assert.Equal(Adventure.VisitScore, a.Score);
        Assert.Equal(2, a.Visited.Count);
    }

    [Fact]
    public void A_locked_door_stays_shut_without_its_key()
    {
        var a = new Adventure();
        a.Teleport("armoury", 0.5f, 0.8f);
        GoThrough(a, Exit(a, "chapel"), 300);
        Assert.Equal("armoury", a.RoomId);
        Assert.NotNull(a.Message);
        Assert.Contains("RED KEY", a.Message());
    }

    [Fact]
    public void The_right_key_opens_the_door_and_stays_in_the_lock()
    {
        var a = new Adventure();
        a.Give(ItemKind.Key, OricColor.Red);
        a.Teleport("armoury", 0.5f, 0.8f);
        int before = a.Score;
        GoThrough(a, Exit(a, "chapel"));
        Assert.Equal("chapel", a.RoomId);
        Assert.False(a.HasKey(OricColor.Red));
        Assert.True(a.Score >= before + Adventure.UnlockScore);
        var door = a.Castle.Doors.First(d => d.LockColor == OricColor.Red);
        Assert.True(a.IsUnlocked(door));
        // And it stays open.
        GoThrough(a, Exit(a, "armoury"));
        GoThrough(a, Exit(a, "chapel"));
        Assert.Equal("chapel", a.RoomId);
    }

    [Fact]
    public void A_key_of_another_colour_does_not_open_the_door()
    {
        var a = new Adventure();
        a.Give(ItemKind.Key, OricColor.Green);
        a.Teleport("armoury", 0.5f, 0.8f);
        GoThrough(a, Exit(a, "chapel"), 300);
        Assert.Equal("armoury", a.RoomId);
        Assert.True(a.HasKey(OricColor.Green));
    }

    [Fact]
    public void Leaving_by_the_gate_needs_the_eagle()
    {
        var a = new Adventure();
        GoThrough(a, a.Castle.Gate.A, 300);
        Assert.Equal(Outcome.Playing, a.Outcome);
        Assert.Equal(Castle.Start, a.RoomId);

        a.Give(ItemKind.Eagle);
        GoThrough(a, a.Castle.Gate.A, 300);
        Assert.Equal(Outcome.Won, a.Outcome);
        Assert.True(a.Score >= Adventure.EscapeScore);
    }

    [Fact]
    public void Taking_and_dropping_an_object()
    {
        var a = new Adventure();
        var bread = a.ItemsIn(Castle.Start).Single(i => i.Kind == ItemKind.Bread);
        a.Tick(new Command { Walk = new WalkTarget(bread.X, bread.Z, bread) });
        Run(a, new Command(), 300);
        Assert.Contains(bread, a.Inventory);
        Assert.Null(bread.Room);

        Run(a, Move(1, 0), 20);
        a.Tick(new Command { Drop = true });
        Assert.Empty(a.Inventory);
        Assert.Equal(Castle.Start, bread.Room);
        Assert.Equal(a.X, bread.X, 3);
    }

    [Fact]
    public void Take_with_nothing_in_reach_does_nothing()
    {
        var a = new Adventure();
        a.Tick(new Command { Take = true });
        Assert.Empty(a.Inventory);
    }

    [Fact]
    public void Treasure_is_scored_and_not_carried()
    {
        var a = new Adventure();
        a.Teleport("greathall", 0.55f, 0.5f);
        var goblet = a.ItemsIn("greathall").Single(i => i.Kind == ItemKind.Goblet);
        int before = a.Score;
        a.Tick(new Command { Walk = new WalkTarget(goblet.X, goblet.Z, goblet) });
        Run(a, new Command(), 100);
        Assert.Empty(a.Inventory);
        Assert.Equal(before + ItemInfo.TreasureValue(ItemKind.Goblet), a.Score);
        Assert.DoesNotContain(goblet, a.AllItems);
    }

    [Fact]
    public void The_hero_can_only_carry_four_things()
    {
        var a = new Adventure();
        a.Give(ItemKind.Lamp);
        a.Give(ItemKind.Cross);
        a.Give(ItemKind.Key, OricColor.Red);
        a.Give(ItemKind.Key, OricColor.Blue);
        Assert.Equal(Adventure.Capacity, a.Inventory.Count);
        var bread = a.ItemsIn(Castle.Start).Single(i => i.Kind == ItemKind.Bread);
        Assert.False(a.Take(bread) && a.Inventory.Count > Adventure.Capacity);
        a.Teleport(Castle.Start, bread.X, bread.Z);
        Assert.False(a.Take(bread));
        Assert.Equal(Castle.Start, bread.Room);
    }

    [Fact]
    public void Bread_restores_energy()
    {
        var a = new Adventure();
        a.SetEnergy(40);
        a.Give(ItemKind.Bread);
        a.Tick(new Command { Use = true });
        Assert.Equal(70, a.Energy);
        Assert.Empty(a.Inventory);
    }

    [Fact]
    public void One_potion_colour_poisons_and_the_others_heal()
    {
        var a = new Adventure(seed: 7);
        var colours = a.AllItems.Where(i => i.Kind == ItemKind.Potion).Select(i => i.Color).Distinct().ToList();
        Assert.Equal(1, colours.Count(c => a.PotionEffectOf(c) == PotionEffect.Poison));

        foreach (var colour in colours)
        {
            var b = new Adventure(seed: 7);
            b.SetEnergy(50);
            b.Give(ItemKind.Potion, colour);
            b.Tick(new Command { Use = true });
            Assert.Equal(b.PotionEffectOf(colour) == PotionEffect.Heal ? 90 : 20, b.Energy);
        }
    }

    [Fact]
    public void The_poison_changes_from_game_to_game()
    {
        var poisons = Enumerable.Range(0, 40)
            .Select(seed => new Adventure(seed: seed))
            .Select(a => a.AllItems.Where(i => i.Kind == ItemKind.Potion).First(i => a.PotionEffectOf(i.Color) == PotionEffect.Poison).Color)
            .Distinct()
            .Count();
        Assert.True(poisons > 1);
    }

    [Fact]
    public void Energy_drains_slowly_and_running_out_kills()
    {
        var a = new Adventure();
        Run(a, new Command(), Adventure.DrainTicks * 3);
        Assert.Equal(Adventure.MaxEnergy - 3, a.Energy);
        a.SetEnergy(1);
        Run(a, new Command(), Adventure.DrainTicks);
        Assert.Equal(Outcome.DiedOfExhaustion, a.Outcome);
    }

    [Fact]
    public void A_trapdoor_drops_the_hero_into_the_room_below()
    {
        var a = new Adventure();
        a.Teleport("guardroom", 0.45f, 0.2f);
        int energy = a.Energy;
        Run(a, Move(0, 1), 30);
        Assert.Equal("oubliette", a.RoomId);
        Assert.Equal(energy - 10, a.Energy);
    }

    [Fact]
    public void The_bottomless_pit_in_the_catacombs_is_deadly()
    {
        var a = new Adventure();
        a.Teleport("catacombs", -0.1f, 0.2f);
        Run(a, Move(0, 1), 40);
        Assert.Equal(Outcome.FellIntoAbyss, a.Outcome);
    }

    [Fact]
    public void Walking_to_a_point_goes_around_a_visible_trapdoor()
    {
        var a = new Adventure();
        a.Teleport("guardroom", 0.45f, 0.1f);
        a.Tick(new Command { Walk = new WalkTarget(0.45f, 0.7f) });
        Run(a, new Command(), 400);
        Assert.Equal("guardroom", a.RoomId);
        Assert.Equal(0.7f, a.Z, 2);
    }

    [Fact]
    public void Dark_rooms_hide_their_objects_until_the_lamp_is_carried()
    {
        var a = new Adventure();
        Assert.False(a.CanSee("catacombs"));
        var key = a.ItemsIn("catacombs").Single(i => i.Kind == ItemKind.Key);
        a.Teleport("catacombs", key.X, key.Z);
        Assert.False(a.Take(key));
        a.Give(ItemKind.Lamp);
        Assert.True(a.CanSee("catacombs"));
        Assert.True(a.Take(key));
    }

    [Fact]
    public void Ghosts_chase_the_hero_and_flee_from_the_cross()
    {
        var a = new Adventure();
        a.Invulnerable = true;
        a.Teleport("crypt", 0.6f, 0.2f);
        var ghost = a.MonstersIn("crypt").Single();
        float d0 = Adventure.Distance(ghost.X, ghost.Z, a.X, a.Z);
        Run(a, new Command(), 60);
        Assert.True(Adventure.Distance(ghost.X, ghost.Z, a.X, a.Z) < d0);

        a.Give(ItemKind.Cross);
        float d1 = Adventure.Distance(ghost.X, ghost.Z, a.X, a.Z);
        Run(a, new Command(), 40);
        Assert.True(ghost.Fleeing);
        Assert.True(Adventure.Distance(ghost.X, ghost.Z, a.X, a.Z) > d1);
    }

    [Fact]
    public void A_ghost_drains_energy_when_it_catches_the_hero()
    {
        var a = new Adventure();
        a.Teleport("crypt", -0.4f, 0.55f);
        Run(a, new Command(), 200);
        Assert.True(a.Energy < Adventure.MaxEnergy - 2);
    }

    [Fact]
    public void Guards_patrol_and_push_the_hero_back()
    {
        var a = new Adventure();
        a.Teleport("armoury", 0.68f, 0.55f);
        var knight = a.MonstersIn("armoury").Single();
        float x0 = knight.X;
        Run(a, new Command(), 60);
        Assert.NotEqual(x0, knight.X);
        Assert.True(a.Energy < Adventure.MaxEnergy);
        Assert.True(a.HurtTicks < 60);
    }

    [Fact]
    public void Sounds_are_played_for_events()
    {
        var sound = new RecordingSound();
        var a = new Adventure(sound);
        var bread = a.ItemsIn(Castle.Start).Single(i => i.Kind == ItemKind.Bread);
        a.Teleport(Castle.Start, bread.X, bread.Z);
        a.Take(bread);
        Assert.Contains(Sfx.Take, sound.Played);
        GoThrough(a, Exit(a, "guardroom"));
        Assert.Contains(Sfx.Door, sound.Played);
    }

    [Fact]
    public void Selecting_slots()
    {
        var a = new Adventure();
        a.Give(ItemKind.Lamp);
        a.Give(ItemKind.Cross);
        a.Tick(new Command { Slot = 0 });
        Assert.Equal(ItemKind.Lamp, a.SelectedItem.Kind);
        a.Tick(new Command { NextSlot = true });
        Assert.Equal(ItemKind.Cross, a.SelectedItem.Kind);
    }

    private sealed class RecordingSound : ISoundPlayer
    {
        public System.Collections.Generic.List<Sfx> Played { get; } = new();
        public void Play(Sfx sfx, float pitch = 0f) => Played.Add(sfx);
    }
}
