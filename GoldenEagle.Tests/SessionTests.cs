using System;
using System.IO;
using GoldenEagle.Core;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Session;
using GoldenEagle.Core.World;

namespace GoldenEagle.Tests;

public class SessionTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "goldeneagle-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void High_scores_are_kept_in_order_and_saved()
    {
        var t = new HighScoreTable(_dir);
        Assert.Equal(HighScoreTable.Capacity, t.Entries.Count);
        int rank = t.Add(99999, "eagle!");
        Assert.Equal(0, rank);
        Assert.Equal("EAGLE", t.Entries[0].Name);
        Assert.Equal(-1, t.Add(1, "LOSER"));
        t.Save();

        var u = new HighScoreTable(_dir);
        u.Load();
        Assert.Equal(99999, u.Best);
        Assert.Equal(HighScoreTable.Capacity, u.Entries.Count);
    }

    [Fact]
    public void A_corrupt_score_file_falls_back_to_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "halloffame.txt"), "garbage\n\t\t\n");
        var t = new HighScoreTable(_dir);
        t.Load();
        Assert.Equal(HighScoreTable.Capacity, t.Entries.Count);
    }

    [Fact]
    public void Settings_remember_the_language_and_volume()
    {
        var s = GameSettings.Load(_dir, "en");
        Assert.Equal(Language.English, s.Language);
        s.Language = Language.French;
        s.Volume = 2;
        s.Save();
        var t = GameSettings.Load(_dir, "en");
        Assert.Equal(Language.French, t.Language);
        Assert.Equal(2, t.Volume);
    }

    [Fact]
    public void The_first_run_uses_the_device_language()
    {
        Assert.Equal(Language.French, GameSettings.Load(_dir, "fr").Language);
        Assert.Equal(Language.English, GameSettings.Load(_dir, "ja").Language);
    }

    [Fact]
    public void Pathing_detours_round_a_trapdoor_and_not_otherwise()
    {
        var pits = new[] { new Pit(-0.2f, 0.4f, 0.2f, 0.6f, null) };
        Assert.Null(Pathing.Detour(-0.8f, 0.1f, -0.8f, 0.9f, pits));
        var w = Pathing.Detour(0f, 0.1f, 0f, 0.9f, pits);
        Assert.NotNull(w);
        Assert.False(pits[0].Contains(w.Value.x, w.Value.z));
        Assert.False(Pathing.SegmentHitsRect(0f, 0.1f, w.Value.x, w.Value.z, -0.32f, 0.33f, 0.32f, 0.67f));
    }
}
