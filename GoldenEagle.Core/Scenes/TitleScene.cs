using System;
using Microsoft.Xna.Framework;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Core.Scenes;

/// <summary>The title: the heraldic golden eagle, the name in both languages and the credits.</summary>
public sealed class TitleScene : Scene
{
    private const int DemoAfter = 15 * 50;

    public TitleScene(GoldenEagleGame game) : base(game)
    {
    }

    public override void Enter() => Game.Sound.PlayTune();

    public override void Leave() => Game.Sound.StopTune();

    protected override void Update()
    {
        var input = Game.Input;
        if (input.LanguagePressed)
            Game.ToggleLanguage();
        if (input.BackPressed)
        {
            Game.Quit();
            return;
        }
        if (Ticks > 10 && (input.ConfirmPressed || input.Taps.Count > 0))
            Game.ShowMenu();
        else if (Ticks > DemoAfter)
            Game.ShowDemo();
    }

    public override void Draw(Canvas c)
    {
        Backdrop.Draw(c, Ticks);
        DrawEagle(c, 6, Ticks);
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(Strings.Title, 2)) / 2, 92, Strings.Title, new Color(255, 214, 90), 2);
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(Strings.Subtitle)) / 2, 112, Strings.Subtitle, new Color(150, 220, 255));
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(Strings.Credit)) / 2, 192, Strings.Credit, Color.White);
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(Strings.BasedOn)) / 2, 202, Strings.BasedOn, new Color(170, 230, 170));
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(Strings.Author)) / 2, 212, Strings.Author, new Color(170, 230, 170));
        float pulse = 0.6f + 0.4f * MathF.Sin(Ticks * 0.12f);
        string start = Strings.PressToStart(Game.IsMobile);
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(start)) / 2, 126, start, new Color(255, 120, 255) * pulse);
    }

    /// <summary>The great golden eagle, glowing, with a glint running over its wings.</summary>
    public static void DrawEagle(Canvas c, float y, int ticks, float scale = 3f)
    {
        var eagle = Sprite.Of(SpriteData.TitleEagle);
        float w = eagle.Width * scale, h = eagle.Height * scale;
        float x = (Canvas.Width - w) / 2;
        float pulse = 0.8f + 0.2f * MathF.Sin(ticks * 0.06f);
        c.Glow(x + w / 2, y + h / 2, w * 0.45f, new Color(255, 190, 60) * 0.35f * pulse);
        c.DrawScaled(eagle, new RectF(x, y, w, h));
        float sweep = ticks * 2.2f % (w + 160) - 80;
        c.Glow(x + sweep, y + h * 0.3f, 10 * scale / 3, Color.White * 0.35f);
    }
}
