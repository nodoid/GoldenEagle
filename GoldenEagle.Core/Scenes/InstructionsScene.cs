using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Core.Scenes;

/// <summary>Three pages: the story, the rules and the controls.</summary>
public sealed class InstructionsScene : Scene
{
    public const int Pages = 3;
    private readonly int _page;

    public InstructionsScene(GoldenEagleGame game, int page = 0) : base(game)
    {
        _page = page;
    }

    protected override void Update()
    {
        var input = Game.Input;
        if (input.LanguagePressed)
            Game.ToggleLanguage();
        if (input.BackPressed)
        {
            Game.ShowMenu();
            return;
        }
        if (Ticks > 10 && (input.ConfirmPressed || input.Taps.Count > 0 || input.RightPressed))
        {
            if (_page + 1 < Pages)
                Game.SwitchTo(new InstructionsScene(Game, _page + 1));
            else
                Game.ShowMenu();
        }
        else if (Ticks > 10 && input.LeftPressed && _page > 0)
        {
            Game.SwitchTo(new InstructionsScene(Game, _page - 1));
        }
    }

    public override void Draw(Canvas c)
    {
        c.TextCentered(2, Strings.Title, OricColor.Yellow, 2);
        switch (_page)
        {
            case 0:
                Header(c, Strings.StoryHeader);
                Lines(c, Strings.Story, 40);
                break;
            case 1:
                Header(c, Strings.RulesHeader);
                Lines(c, Strings.Rules, 40);
                break;
            default:
                Header(c, Strings.ControlsHeader);
                var controls = Strings.Controls(Game.IsMobile);
                for (int i = 0; i < controls.Length; i++)
                {
                    c.TextAt(1, 42 + i * 12, controls[i].key, OricColor.Yellow);
                    c.TextAt(20, 42 + i * 12, controls[i].action, OricColor.White);
                }
                break;
        }
        c.TextCentered(200, Strings.Page(_page + 1, Pages), OricColor.Cyan);
        if (Ticks / 25 % 2 == 0)
            c.TextCentered(212, Strings.NextPage(Game.IsMobile), OricColor.Magenta);
    }

    private static void Header(Canvas c, string text)
    {
        c.TextCentered(24, text, OricColor.Cyan);
        int w = text.Length * 6;
        c.Fill((Canvas.Width - w) / 2, 33, w, 1, OricColor.Cyan);
    }

    private static void Lines(Canvas c, string[] lines, int y)
    {
        for (int i = 0; i < lines.Length; i++)
            c.TextAt(1, y + i * 12, lines[i], i % 2 == 0 ? OricColor.White : OricColor.White);
    }
}
