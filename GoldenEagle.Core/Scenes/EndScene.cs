using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Core.Scenes;

/// <summary>The end of a game: victory with the eagle, or the manner of the hero's death.</summary>
public sealed class EndScene : Scene
{
    private readonly Adventure _adventure;

    public EndScene(GoldenEagleGame game, Adventure adventure) : base(game)
    {
        _adventure = adventure;
    }

    public bool Won => _adventure.Outcome == Outcome.Won;

    protected override void Update()
    {
        var input = Game.Input;
        if (input.LanguagePressed)
            Game.ToggleLanguage();
        if (Ticks > 60 && (input.ConfirmPressed || input.Taps.Count > 0 || input.BackPressed) || Ticks > 600)
            Game.ShowHallOfFame(_adventure);
    }

    public override void Draw(Canvas c)
    {
        Backdrop.Draw(c, Ticks, 0.35f);
        if (Won)
        {
            TitleScene.DrawEagle(c, 6, Ticks);
            c.TextCentered(94, Strings.Victory, OricColor.Yellow, 2);
            c.TextCentered(116, Strings.VictoryText1, OricColor.White);
            c.TextCentered(126, Strings.VictoryText2, OricColor.Yellow);
        }
        else
        {
            c.TextCentered(30, Strings.GameOver, OricColor.Red, 2);
            string why = _adventure.Outcome == Outcome.FellIntoAbyss ? Strings.DeathAbyss : Strings.DeathEnergy;
            c.TextCentered(62, why, OricColor.White);
            var ghost = Sprite.Of(SpriteData.Ghost0);
            int bob = Ticks / 8 % 2 * 2;
            c.DrawScaled(ghost, new Microsoft.Xna.Framework.Rectangle(108, 80 + bob, 24, 28));
        }

        int total = _adventure.Castle.Rooms.Count;
        c.TextCentered(146, Strings.RoomsExplored(_adventure.Visited.Count, total), OricColor.Cyan);
        c.TextCentered(158, Strings.TimeTaken(_adventure.Seconds), OricColor.Cyan);
        c.TextCentered(176, Strings.YourScore + " " + _adventure.Score.ToString("D6"), OricColor.Yellow);
        if (Ticks > 60 && Ticks / 25 % 2 == 0)
            c.TextCentered(204, Strings.NextPage(Game.IsMobile), OricColor.Magenta);
    }
}
