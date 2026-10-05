using System;
using System.IO;
using System.Linq;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.World;

namespace GoldenEagle.Tests;

/// <summary>The high-resolution pictures exist for everything drawn, in the proportions of their layout maps.</summary>
public class ArtTests
{
    private static (int w, int h)? PngSize(string name)
    {
        using var s = typeof(Canvas).Assembly.GetManifestResourceStream("Art." + name + ".png");
        if (s == null)
            return null;
        var header = new byte[24];
        s.ReadExactly(header);
        int Be(int o) => header[o] << 24 | header[o + 1] << 16 | header[o + 2] << 8 | header[o + 3];
        return (Be(16), Be(20));
    }

    private static void AssertArt(string name, string[] map)
    {
        var size = PngSize(name);
        Assert.True(size != null, $"Art/{name}.png is missing");
        float mapAspect = map.Max(r => r.Length) / (float)map.Length;
        float artAspect = size.Value.w / (float)size.Value.h;
        Assert.True(Math.Abs(mapAspect - artAspect) < 0.03f * mapAspect, $"{name}: {artAspect} vs {mapAspect}");
        Assert.True(size.Value.h >= map.Length * 8, $"{name} is not high resolution");
    }

    [Fact]
    public void Every_sprite_map_has_a_high_resolution_picture()
    {
        foreach (var f in typeof(SpriteData).GetFields().Where(f => f.FieldType == typeof(string[])))
        {
            if (f.Name is nameof(SpriteData.TitleEagleHalf) or nameof(SpriteData.Key) or nameof(SpriteData.Potion) or nameof(SpriteData.Jewels))
                continue;
            AssertArt(f.Name, (string[])f.GetValue(null));
        }
    }

    [Fact]
    public void Every_coloured_object_in_the_castle_has_its_picture()
    {
        foreach (var item in Castle.Build().Rooms.SelectMany(r => r.Items))
        {
            var sprite = RoomRenderer.ItemSprite(item.Kind, item.Color);
            AssertArt(sprite.Name, RoomRenderer.ItemArt(item.Kind));
        }
    }

    [Fact]
    public void Sprite_maps_take_their_field_names()
    {
        Assert.Equal("HeroSide1", Sprite.Of(SpriteData.HeroSide1, "ignored").Name);
        Assert.Equal("Key-Red", Sprite.Of(SpriteData.Key).Recolor(Core.Oric.OricColor.Red).Name);
    }
}
