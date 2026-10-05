using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Core.Graphics;

/// <summary>
/// Draws into the 240x224 virtual Oric screen: lines, filled shapes, sprites (also scaled for
/// depth) and ROM-font text. All coordinates are in Oric pixels.
/// </summary>
public sealed class Canvas : IDisposable
{
    public const int Width = 240;
    public const int Height = 224;
    public const int Columns = Width / OricFont.GlyphWidth;   // 40
    public const int Rows = Height / OricFont.GlyphHeight;    // 28

    private readonly GraphicsDevice _device;
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly Texture2D _font;
    private readonly Dictionary<Sprite, Texture2D> _textures = new();

    public Canvas(GraphicsDevice device)
    {
        _device = device;
        _batch = new SpriteBatch(device);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData([Color.White]);
        _font = BuildFont(device);
    }

    public void Begin() => _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

    public void End() => _batch.End();

    public void Fill(int x, int y, int w, int h, OricColor color)
    {
        if (w <= 0 || h <= 0)
            return;
        _batch.Draw(_pixel, new Rectangle(x, y, w, h), color.ToColor());
    }

    public void Plot(int x, int y, OricColor color) => Fill(x, y, 1, 1, color);

    /// <summary>Outline of a rectangle, one pixel wide.</summary>
    public void Frame(int x, int y, int w, int h, OricColor color)
    {
        Fill(x, y, w, 1, color);
        Fill(x, y + h - 1, w, 1, color);
        Fill(x, y, 1, h, color);
        Fill(x + w - 1, y, 1, h, color);
    }

    /// <summary>Bresenham line, as drawn by the Oric's DRAW command.</summary>
    public void Line(int x0, int y0, int x1, int y1, OricColor color)
    {
        if (y0 == y1)
        {
            Fill(Math.Min(x0, x1), y0, Math.Abs(x1 - x0) + 1, 1, color);
            return;
        }
        if (x0 == x1)
        {
            Fill(x0, Math.Min(y0, y1), 1, Math.Abs(y1 - y0) + 1, color);
            return;
        }
        foreach (var (x, y) in Geometry.LinePoints(x0, y0, x1, y1))
            Plot(x, y, color);
    }

    public void Line(Point a, Point b, OricColor color) => Line(a.X, a.Y, b.X, b.Y, color);

    /// <summary>Fills a convex polygon with horizontal spans.</summary>
    public void FillPolygon(ReadOnlySpan<Point> points, OricColor color)
    {
        foreach (var (y, x0, x1) in Geometry.Spans(points))
            Fill(x0, y, x1 - x0 + 1, 1, color);
    }

    /// <summary>Fills a convex polygon with a two-colour dither (alternate pixels), for shading.</summary>
    public void DitherPolygon(ReadOnlySpan<Point> points, OricColor a, OricColor b)
    {
        foreach (var (y, x0, x1) in Geometry.Spans(points))
        {
            Fill(x0, y, x1 - x0 + 1, 1, a);
            for (int x = x0 + ((x0 + y) & 1); x <= x1; x += 2)
                Plot(x, y, b);
        }
    }

    public void Draw(Sprite sprite, int x, int y)
    {
        _batch.Draw(TextureFor(sprite), new Vector2(x, y), Color.White);
    }

    /// <summary>Draws a sprite stretched (point sampled) into a rectangle: used for perspective.</summary>
    public void DrawScaled(Sprite sprite, Rectangle destination, bool mirror = false)
    {
        if (destination.Width <= 0 || destination.Height <= 0)
            return;
        _batch.Draw(TextureFor(sprite), destination, null, Color.White, 0f, Vector2.Zero,
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    /// <summary>Draws a sprite with every solid pixel in a single colour.</summary>
    public void DrawTinted(Sprite sprite, int x, int y, OricColor color)
    {
        _batch.Draw(TextureFor(sprite.Recolor(OricColor.White)), new Vector2(x, y), color.ToColor());
    }

    public void DrawScaledTinted(Sprite sprite, Rectangle destination, OricColor color, bool mirror = false)
    {
        if (destination.Width <= 0 || destination.Height <= 0)
            return;
        _batch.Draw(TextureFor(sprite.Recolor(OricColor.White)), destination, null, color.ToColor(), 0f, Vector2.Zero,
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    /// <summary>Text at a pixel position, transparent paper.</summary>
    public void Text(int x, int y, string text, OricColor ink, int scale = 1)
    {
        var tint = ink.ToColor();
        for (int i = 0; i < text.Length; i++)
        {
            int index = OricFont.IndexOf(text[i]);
            if (index == 0)
                continue;
            var src = new Rectangle(index * OricFont.GlyphWidth, 0, OricFont.GlyphWidth, OricFont.GlyphHeight);
            var dst = new Rectangle(x + i * OricFont.GlyphWidth * scale, y, OricFont.GlyphWidth * scale, OricFont.GlyphHeight * scale);
            _batch.Draw(_font, dst, src, tint);
        }
    }

    /// <summary>Text at a character cell (40 columns).</summary>
    public void TextAt(int column, int y, string text, OricColor ink) =>
        Text(column * OricFont.GlyphWidth, y, text, ink);

    /// <summary>Text horizontally centred on the 40 column screen.</summary>
    public void TextCentered(int y, string text, OricColor ink, int scale = 1) =>
        Text((Width - text.Length * OricFont.GlyphWidth * scale) / 2, y, text, ink, scale);

    private Texture2D TextureFor(Sprite sprite)
    {
        if (_textures.TryGetValue(sprite, out var tex))
            return tex;
        var data = new Color[sprite.Width * sprite.Height];
        for (int i = 0; i < data.Length; i++)
        {
            byte p = sprite.Pixels[i];
            data[i] = p == 0 ? Color.Transparent : OricPalette.Colors[p - 1];
        }
        tex = new Texture2D(_device, sprite.Width, sprite.Height);
        tex.SetData(data);
        _textures[sprite] = tex;
        return tex;
    }

    private static Texture2D BuildFont(GraphicsDevice device)
    {
        int count = OricFont.GlyphCount;
        int w = count * OricFont.GlyphWidth;
        int h = OricFont.GlyphHeight;
        var data = new Color[w * h];
        for (int g = 0; g < count; g++)
        {
            var rows = OricFont.GlyphAt(g);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < OricFont.GlyphWidth; x++)
                    if ((rows[y] & (1 << (OricFont.GlyphWidth - 1 - x))) != 0)
                        data[y * w + g * OricFont.GlyphWidth + x] = Color.White;
        }
        var tex = new Texture2D(device, w, h);
        tex.SetData(data);
        return tex;
    }

    public void Dispose()
    {
        foreach (var t in _textures.Values)
            t.Dispose();
        _textures.Clear();
        _pixel.Dispose();
        _font.Dispose();
        _batch.Dispose();
    }
}

/// <summary>Pure raster helpers (testable without a graphics device).</summary>
public static class Geometry
{
    public static IEnumerable<(int x, int y)> LinePoints(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            yield return (x0, y0);
            if (x0 == x1 && y0 == y1)
                yield break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>Horizontal spans (y, xLeft, xRight) covering a convex polygon, clipped to the screen.</summary>
    public static List<(int y, int x0, int x1)> Spans(ReadOnlySpan<Point> pts)
    {
        var result = new List<(int, int, int)>();
        if (pts.Length < 3)
            return result;
        int top = int.MaxValue, bottom = int.MinValue;
        foreach (var p in pts)
        {
            top = Math.Min(top, p.Y);
            bottom = Math.Max(bottom, p.Y);
        }
        top = Math.Max(top, 0);
        bottom = Math.Min(bottom, Canvas.Height - 1);
        for (int y = top; y <= bottom; y++)
        {
            float left = float.MaxValue, right = float.MinValue;
            float yc = y + 0.5f;
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % pts.Length];
                if (a.Y == b.Y)
                {
                    if (a.Y == y)
                    {
                        left = Math.Min(left, Math.Min(a.X, b.X));
                        right = Math.Max(right, Math.Max(a.X, b.X));
                    }
                    continue;
                }
                float lo = Math.Min(a.Y, b.Y), hi = Math.Max(a.Y, b.Y);
                if (yc < lo || yc > hi)
                    continue;
                float x = a.X + (yc - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
            if (left > right)
                continue;
            int x0 = Math.Max(0, (int)Math.Round(left));
            int x1 = Math.Min(Canvas.Width - 1, (int)Math.Round(right));
            if (x0 <= x1)
                result.Add((y, x0, x1));
        }
        return result;
    }
}
