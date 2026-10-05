using System;
using GoldenEagle.Core.Audio;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;
using GoldenEagle.Core.Scenes;

namespace GoldenEagle.Core.Demo;

/// <summary>The castle played by the <see cref="Autopilot"/>: the attract mode and the store captures.</summary>
public sealed class DemoScene : Scene
{
    private readonly Autopilot _pilot;
    private readonly bool _attract;

    public DemoScene(GoldenEagleGame game, ISoundPlayer sound, bool attract, int seed = 1984) : base(game)
    {
        Adventure = new Adventure(sound, seed);
        _pilot = new Autopilot(Adventure);
        _attract = attract;
    }

    public Adventure Adventure { get; }

    protected override void Update()
    {
        if (_attract)
        {
            var input = Game.Input;
            if (input.ConfirmPressed || input.Taps.Count > 0 || input.BackPressed || Ticks > 90 * 50 ||
                Adventure.Outcome != Outcome.Playing)
            {
                Game.ShowTitle();
                return;
            }
        }
        Adventure.Tick(_pilot.Next());
    }

    public override void Draw(Canvas c)
    {
        RoomRenderer.Draw(c, Adventure, Ticks, Game.IsMobile);
        if (_attract && Ticks / 25 % 2 == 0)
        {
            string demo = "DEMO";
            c.Fill(Canvas.Width - demo.Length * 6 - 6, 2, demo.Length * 6 + 4, 10, OricColor.Red);
            c.Text(Canvas.Width - demo.Length * 6 - 4, 3, demo, OricColor.White);
        }
    }

    /// <summary>Runs the demo without drawing until <paramref name="ready"/> holds (store captures).</summary>
    public void FastForward(Func<Adventure, bool> ready, int limit = 100000)
    {
        for (int i = 0; i < limit && !ready(Adventure) && Adventure.Outcome == Outcome.Playing; i++)
            Tick();
    }
}
