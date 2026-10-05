using System;
using System.Linq;
using Microsoft.Xna.Framework;
using GoldenEagle.Core;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Tests;

public class GraphicsTests
{
    [Fact]
    public void The_font_draws_French_accents()
    {
        foreach (char c in "éèêëàâîïôùûçÉÈÊÀÇÔ")
        {
            Assert.True(OricFont.CanDraw(c), c.ToString());
            Assert.True(OricFont.IndexOf(c) >= OricFont.CharCount);
        }
        // The accent sits above the letter: é has a pixel in its top row, e does not.
        Assert.Equal(0, OricFont.Glyph('e')[0]);
        Assert.NotEqual(0, OricFont.Glyph('é')[0]);
        Assert.Equal(OricFont.Glyph('e')[3], OricFont.Glyph('é')[3]);
    }

    [Fact]
    public void Unknown_characters_draw_as_spaces()
    {
        Assert.False(OricFont.CanDraw('€'));
        Assert.Equal(0, OricFont.IndexOf('€'));
    }

    [Fact]
    public void Lines_join_their_end_points()
    {
        var pts = Geometry.LinePoints(3, 4, 20, 11).ToList();
        Assert.Equal((3, 4), pts[0]);
        Assert.Equal((20, 11), pts[^1]);
        Assert.Equal(18, pts.Count);
    }

    [Fact]
    public void Polygon_spans_cover_a_rectangle_exactly()
    {
        var spans = Geometry.Spans([new Point(10, 10), new Point(20, 10), new Point(20, 15), new Point(10, 15)]);
        Assert.Equal(6, spans.Count);
        Assert.All(spans, s => Assert.Equal((10, 20), (s.x0, s.x1)));
    }

    [Fact]
    public void Polygon_spans_are_clipped_to_the_screen()
    {
        var spans = Geometry.Spans([new Point(-50, -10), new Point(300, -10), new Point(300, 400), new Point(-50, 400)]);
        Assert.Equal(Canvas.Height, spans.Count);
        Assert.All(spans, s => Assert.Equal((0, Canvas.Width - 1), (s.x0, s.x1)));
    }

    [Fact]
    public void The_front_of_the_room_fills_the_picture_and_the_back_wall_is_smaller()
    {
        var frontLeft = Perspective.Project(-1, 0);
        var backLeft = Perspective.Project(-1, 1);
        Assert.Equal(Perspective.FrontFloorY, frontLeft.Y);
        Assert.InRange(frontLeft.X, 0, 8);
        Assert.True(backLeft.X > frontLeft.X + 50);
        Assert.True(backLeft.Y < frontLeft.Y);
        Assert.Equal(Perspective.BackScale, Perspective.Scale(1), 3);
        Assert.True(Perspective.Project(-1, 0, 1).Y >= 0);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(-0.7f, 0.3f)]
    [InlineData(0.5f, 0.9f)]
    [InlineData(0.86f, 0.05f)]
    public void Screen_points_map_back_to_the_floor(float x, float z)
    {
        var p = Perspective.Project(x, z);
        var floor = Perspective.ToFloor(p.X, p.Y);
        Assert.NotNull(floor);
        Assert.Equal(x, floor.Value.x, 1);
        Assert.Equal(z, floor.Value.z, 1);
    }

    [Fact]
    public void Points_above_the_floor_are_not_on_it()
    {
        Assert.Null(Perspective.ToFloor(120, 20));
    }

    [Fact]
    public void Near_things_are_drawn_bigger()
    {
        var near = Perspective.SpriteRect(12, 24, 0, 0.1f);
        var far = Perspective.SpriteRect(12, 24, 0, 0.9f);
        Assert.True(near.Height > far.Height * 1.8);
        Assert.True(near.Bottom > far.Bottom);
    }

    [Fact]
    public void Every_sprite_uses_only_Oric_colours()
    {
        foreach (var f in typeof(SpriteData).GetFields().Where(f => f.FieldType == typeof(string[])))
        {
            var rows = (string[])f.GetValue(null);
            foreach (var row in rows)
                foreach (char ch in row)
                    Assert.True(ch == '.' || OricPalette.FromCode(ch) != null, $"{f.Name}: '{ch}'");
            Assert.True(new Sprite(f.Name, rows).Width > 0);
        }
    }

    [Fact]
    public void Animation_frames_match_in_size()
    {
        void Same(string[] a, string[] b)
        {
            Assert.Equal(a.Length, b.Length);
            Assert.Equal(a.Max(r => r.Length), b.Max(r => r.Length));
        }
        Same(SpriteData.HeroFront0, SpriteData.HeroFront1);
        Same(SpriteData.HeroBack0, SpriteData.HeroBack1);
        Same(SpriteData.HeroSide0, SpriteData.HeroSide1);
        Same(SpriteData.HeroFront0, SpriteData.HeroSide0);
        Same(SpriteData.Ghost0, SpriteData.Ghost1);
        Same(SpriteData.Bat0, SpriteData.Bat1);
        Same(SpriteData.Knight0, SpriteData.Knight1);
        Same(SpriteData.Torch, SpriteData.TorchFlicker);
    }

    [Fact]
    public void The_title_eagle_is_symmetric()
    {
        foreach (var row in SpriteData.TitleEagle)
        {
            Assert.Equal(SpriteData.TitleEagleHalf[0].Length * 2 - 1, row.Length);
            Assert.Equal(row, new string(row.Reverse().ToArray()));
        }
    }

    [Theory]
    [InlineData(960, 896, true, 0, 0, 960, 896)]
    [InlineData(2532, 1170, false, 39, 0, 1253, 1170)]
    public void The_screen_is_scaled_to_fit(int w, int h, bool integer, int x, int y, int ew, int eh)
    {
        var r = GoldenEagleGame.ComputeDestination(w, h, integer);
        Assert.Equal(new Rectangle(x + (integer ? 0 : (w - ew) / 2 - x), y, ew, eh), r);
    }

    [Fact]
    public void Panel_buttons_and_slots_do_not_overlap()
    {
        var rects = Enumerable.Range(0, Adventure.Capacity).Select(Hud.Slot).Append(Hud.UseButton).Append(Hud.DropButton).ToList();
        for (int i = 0; i < rects.Count; i++)
            for (int j = i + 1; j < rects.Count; j++)
                Assert.False(rects[i].Intersects(rects[j]), $"{i} {j}");
        Assert.All(rects, r => Assert.True(r.Top >= Hud.Top && r.Bottom <= Hud.StatusY && r.Right <= Canvas.Width));
    }
}
