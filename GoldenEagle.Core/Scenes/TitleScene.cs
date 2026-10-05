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
        DrawEagle(c, 8, Ticks);
        c.TextCentered(96, Strings.Title, OricColor.Yellow, 2);
        c.TextCentered(116, Strings.Subtitle, OricColor.Cyan);
        c.TextCentered(146, Strings.Credit, OricColor.White);
        c.TextCentered(160, Strings.BasedOn, OricColor.Green);
        c.TextCentered(170, Strings.Author, OricColor.Green);
        if (Ticks / 25 % 2 == 0)
            c.TextCentered(196, Strings.PressToStart(Game.IsMobile), OricColor.Magenta);
    }

    /// <summary>The big eagle with a sparkle running over it.</summary>
    public static void DrawEagle(Canvas c, int y, int ticks, int scale = 3)
    {
        var eagle = Sprite.Of(SpriteData.TitleEagle, "title-eagle");
        int w = eagle.Width * scale, h = eagle.Height * scale;
        int x = (Canvas.Width - w) / 2;
        c.DrawScaled(eagle, new Microsoft.Xna.Framework.Rectangle(x, y, w, h));
        int sweep = ticks * 3 % (w + 120) - 60;
        for (int py = 0; py < eagle.Height; py++)
        {
            int px = (sweep - py * scale) / scale;
            if (eagle.IsSolid(px, py))
                c.Fill(x + px * scale, y + py * scale, scale, scale, OricColor.White);
        }
    }
}
