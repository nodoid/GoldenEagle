using System;
using Microsoft.Xna.Framework;
using GoldenEagle.Core.Graphics;

namespace GoldenEagle.Core.Scenes;

/// <summary>The night scene behind the title and menus: sky, stars, moon and the castle on its hill.</summary>
public static class Backdrop
{
    private static readonly Vector2[] Stars = MakeStars();

    private static Vector2[] MakeStars()
    {
        var rnd = new Random(1984);
        var stars = new Vector2[70];
        for (int i = 0; i < stars.Length; i++)
            stars[i] = new Vector2((float)rnd.NextDouble() * Canvas.Width, (float)rnd.NextDouble() * 150);
        return stars;
    }

    public static void Draw(Canvas c, int ticks, float dim = 1f)
    {
        c.ShadePolygon([new(0, 0), new(Canvas.Width, 0), new(Canvas.Width, Canvas.Height), new(0, Canvas.Height)],
            new Color(40, 50, 120), (x, y) => (0.25f + 0.75f * y / Canvas.Height) * dim);
        for (int i = 0; i < Stars.Length; i++)
        {
            var s = Stars[i];
            float twinkle = 0.5f + 0.5f * MathF.Sin(ticks * 0.05f + i * 1.7f);
            c.Glow(s.X, s.Y, 1.2f + (i % 3) * 0.4f, Color.White * (0.35f + 0.5f * twinkle) * dim);
        }
        c.Glow(200, 30, 26, new Color(255, 250, 220) * 0.18f * dim);
        c.Glow(200, 30, 9, new Color(255, 252, 235) * dim);

        // The castle on its hill, with lit windows.
        var hill = new Color(16, 14, 30) * dim;
        hill.A = 255;
        c.ShadePolygon([new(0, 190), new(60, 176), new(150, 172), new(240, 184), new(240, 224), new(0, 224)], new Color(22, 20, 40),
            (x, y) => dim);
        var wall = new Color(12, 10, 24);
        Tower(c, 68, 120, 16, 60, wall);
        Tower(c, 156, 116, 18, 64, wall);
        c.FillPolygon([new(80, 150), new(158, 150), new(158, 180), new(80, 180)], wall);
        for (float x = 80; x < 158; x += 6)
            c.Fill(x, 146, 3.5f, 4, wall);
        Tower(c, 108, 96, 22, 84, wall);
        float flicker = 0.75f + 0.25f * MathF.Sin(ticks * 0.21f);
        foreach (var (x, y) in new (float, float)[] { (114, 112), (122, 112), (118, 134), (73, 136), (162, 132), (96, 160), (140, 158) })
        {
            c.Glow(x + 1.2f, y + 2, 5, new Color(255, 170, 60) * 0.35f * flicker * dim);
            c.Fill(x, y, 2.4f, 4, new Color(255, 196, 90) * flicker * dim);
        }
    }

    private static void Tower(Canvas c, float x, float top, float w, float h, Color colour)
    {
        c.FillPolygon([new(x, top), new(x + w, top), new(x + w, top + h), new(x, top + h)], colour);
        c.FillPolygon([new(x - 2, top), new(x + w / 2, top - w * 0.9f), new(x + w + 2, top)], colour);
    }
}
