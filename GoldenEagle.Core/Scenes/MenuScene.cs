using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GoldenEagle.Core.Audio;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Core.Scenes;

/// <summary>Main menu, worked with the cursor keys and SPACE, the mouse or a tap.</summary>
public sealed class MenuScene : Scene
{
    private const int MenuTop = 112;
    private const int ItemHeight = 14;
    private int _selected;

    private enum Item
    {
        Play,
        Instructions,
        HallOfFame,
        Language,
        Volume,
        Quit,
    }

    public MenuScene(GoldenEagleGame game) : base(game)
    {
    }

    private List<Item> Items()
    {
        var items = new List<Item> { Item.Play, Item.Instructions, Item.HallOfFame, Item.Language, Item.Volume };
        if (!Game.IsMobile)
            items.Add(Item.Quit);
        return items;
    }

    private string Text(Item item) => item switch
    {
        Item.Play => Strings.Play,
        Item.Instructions => Strings.Instructions,
        Item.HallOfFame => Strings.HallOfFame,
        Item.Language => Strings.LanguageItem,
        Item.Volume => Strings.Volume + " : " + Strings.VolumeName(Game.Sound.Level),
        _ => Strings.Quit,
    };

    protected override void Update()
    {
        var input = Game.Input;
        var items = Items();
        if (input.LanguagePressed)
            Activate(Item.Language);
        if (input.VolumePressed)
            Activate(Item.Volume);
        if (input.InstructionsPressed)
            Activate(Item.Instructions);
        if (input.BackPressed)
        {
            Game.ShowTitle();
            return;
        }
        if (input.UpPressed)
            _selected = (_selected + items.Count - 1) % items.Count;
        if (input.DownPressed)
            _selected = (_selected + 1) % items.Count;

        if (input.Pointer is { } p && RowAt(p.Y, items.Count) is int hover)
            _selected = hover;
        foreach (var tap in input.Taps)
        {
            if (RowAt(tap.Y, items.Count) is int row)
            {
                _selected = row;
                Activate(items[row]);
                return;
            }
        }
        if (input.Taps.Count == 0 && input.ConfirmPressed)
            Activate(items[_selected]);
    }

    private static int? RowAt(float y, int count)
    {
        int row = (int)((y - MenuTop + 3) / ItemHeight);
        return y >= MenuTop - 3 && row >= 0 && row < count ? row : null;
    }

    private void Activate(Item item)
    {
        switch (item)
        {
            case Item.Play:
                Game.StartNewGame();
                break;
            case Item.Instructions:
                Game.ShowInstructions();
                break;
            case Item.HallOfFame:
                Game.ShowHallOfFame();
                break;
            case Item.Language:
                Game.ToggleLanguage();
                Game.Sound.Play(Sfx.Menu);
                break;
            case Item.Volume:
                Game.CycleVolume();
                break;
            default:
                Game.Quit();
                break;
        }
    }

    public override void Draw(Canvas c)
    {
        Backdrop.Draw(c, Ticks, 0.55f);
        TitleScene.DrawEagle(c, 4, Ticks, 2);
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(Strings.Title, 2)) / 2, 64, Strings.Title, new Color(255, 214, 90), 2);
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(Strings.Subtitle)) / 2, 84, Strings.Subtitle, new Color(150, 220, 255));

        var items = Items();
        for (int i = 0; i < items.Count; i++)
        {
            string text = Text(items[i]);
            int y = MenuTop + i * ItemHeight;
            if (i == _selected)
            {
                // Oric inverse video highlight.
                float w = (text.Length + 2) * 6;
                c.ShadePolygon([new((Canvas.Width - w) / 2, y - 2), new((Canvas.Width + w) / 2, y - 2),
                        new((Canvas.Width + w) / 2, y + 9), new((Canvas.Width - w) / 2, y + 9)],
                    new Color(240, 196, 80), (_, yy) => 1.1f - 0.4f * (yy - y + 2) / 11f);
                c.TextCentered(y, text, new Color(30, 20, 60));
            }
            else
            {
                c.TextCentered(y, text, i == 0 ? OricColor.Yellow : OricColor.White);
            }
        }

        c.TextCentered(204, Strings.Credit, OricColor.Cyan);
        c.TextCentered(214, Strings.BasedOn, new Color(170, 230, 170));
    }
}
