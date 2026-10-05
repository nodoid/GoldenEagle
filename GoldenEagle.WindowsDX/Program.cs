using System;
using GoldenEagle.Core;

namespace GoldenEagle.WindowsDX;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var game = new GoldenEagleGame();
        // --capture <folder> [stills|videos|all] [--mobile] [--lang fr|en] records the store screenshots and videos.
        game.Capture = Core.Demo.CaptureDirector.FromArgs(args);
        game.Run();
    }
}
