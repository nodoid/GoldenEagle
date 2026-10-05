using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;
using GoldenEagle.Core.World;

namespace GoldenEagle.Tests;

[Collection("Language")]
public class LocalizationTests : IDisposable
{
    private readonly Language _saved = Strings.Current;

    public void Dispose() => Strings.Current = _saved;

    /// <summary>Every text the game can show, in one language.</summary>
    private static List<string> AllText(Language language)
    {
        Strings.Current = language;
        var texts = new List<string>();
        foreach (var p in typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            var value = p.GetValue(null);
            if (value is string s)
                texts.Add(s);
            else if (value is string[] lines)
                texts.AddRange(lines);
        }
        foreach (bool mobile in new[] { false, true })
        {
            texts.Add(Strings.PressToStart(mobile));
            texts.Add(Strings.Resume(mobile));
            texts.Add(Strings.ChangeLetter(mobile));
            texts.Add(Strings.AcceptLetter(mobile));
            texts.Add(Strings.PlayOrMenu(mobile));
            texts.Add(Strings.NextPage(mobile));
            foreach (var (k, a) in Strings.Controls(mobile))
            {
                texts.Add(k);
                texts.Add(a);
            }
        }
        for (int v = 0; v < 4; v++)
            texts.Add(Strings.Volume + " : " + Strings.VolumeName(v));
        texts.Add(Strings.RoomsExplored(18, 18));
        texts.Add(Strings.TimeTaken(3599));
        texts.Add(Strings.Page(3, 3));
        texts.Add(Strings.YourScore + " 000000");
        var castle = Castle.Build();
        foreach (var room in castle.Rooms)
            texts.Add(room.Name.Get(language));
        foreach (ItemKind kind in Enum.GetValues<ItemKind>())
            foreach (var c in new[] { OricColor.Red, OricColor.Green, OricColor.Blue, OricColor.Yellow, OricColor.Magenta, OricColor.Cyan })
            {
                string name = ItemInfo.Name(kind, c).Get(language);
                texts.Add(Strings.YouTake(name));
                texts.Add(Strings.YouDrop(name));
                texts.Add(Strings.DoorLocked(ItemInfo.Name(ItemKind.Key, c).Get(language)));
            }
        return texts;
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.French)]
    public void Every_character_can_be_drawn_with_the_Oric_font(Language language)
    {
        foreach (var text in AllText(language))
            foreach (char ch in text)
                Assert.True(OricFont.CanDraw(ch), $"'{ch}' (U+{(int)ch:X4}) in \"{text}\"");
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.French)]
    public void Every_line_fits_the_40_column_screen(Language language)
    {
        foreach (var text in AllText(language))
            Assert.True(text.Length <= 40, $"{text.Length}: \"{text}\"");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Control_tables_fit_their_columns(bool mobile)
    {
        foreach (var language in new[] { Language.English, Language.French })
        {
            Strings.Current = language;
            foreach (var (key, action) in Strings.Controls(mobile))
            {
                Assert.True(key.Length <= 19, key);
                Assert.True(action.Length <= 20, action);
            }
        }
    }

    [Fact]
    public void The_two_languages_differ()
    {
        var en = AllText(Language.English);
        var fr = AllText(Language.French);
        int same = en.Intersect(fr).Count();
        Assert.True(same < en.Count / 5, $"{same} of {en.Count} texts are the same in both languages");
        Assert.Contains("THE GOLDEN EAGLE", en);
        Assert.Contains("L'AIGLE D'OR", fr);
    }

    [Fact]
    public void Message_texts_fit_the_panel_in_both_languages()
    {
        foreach (var language in new[] { Language.English, Language.French })
        {
            Strings.Current = language;
            string[] messages =
            [
                Strings.HandsFull, Strings.NothingHere, Strings.NothingToUse, Strings.DoorUnlocked, Strings.ItIsDark,
                Strings.YouFall, Strings.GhostTouch, Strings.BatBite, Strings.KnightHit, Strings.Healed, Strings.Poisoned,
                Strings.Eaten, Strings.LampLit, Strings.CrossRaised, Strings.KeyHint, Strings.EagleHint, Strings.NeedEagle,
                Strings.Tired, Strings.Treasure(ItemInfo.Name(ItemKind.Jewels, OricColor.Cyan).Get(language), 250),
            ];
            Assert.All(messages, m => Assert.InRange(m.Length, 1, 40));
        }
    }

    [Theory]
    [InlineData("fr", Language.French)]
    [InlineData("FR", Language.French)]
    [InlineData("en", Language.English)]
    [InlineData("de", Language.English)]
    [InlineData(null, Language.English)]
    public void The_device_language_picks_French_or_English(string iso, Language expected)
    {
        Assert.Equal(expected, Strings.FromIso(iso));
    }

    [Fact]
    public void Loc_follows_the_current_language()
    {
        var loc = new Loc("LA CLÉ", "THE KEY");
        Strings.Current = Language.French;
        Assert.Equal("LA CLÉ", loc.ToString());
        Strings.Current = Language.English;
        Assert.Equal("THE KEY", loc.ToString());
    }
}
