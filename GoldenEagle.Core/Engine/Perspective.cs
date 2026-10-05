using System;
using Microsoft.Xna.Framework;
using GoldenEagle.Core.Graphics;

namespace GoldenEagle.Core.Engine;

/// <summary>
/// One-point perspective of a room seen through its open front wall, as in the original:
/// the back wall is a small rectangle in the middle, the side walls, floor and ceiling run
/// out to the edges of the picture. Room space: x -1..1 (left..right wall), z 0..1 (front..back),
/// h 0..1 (floor..ceiling).
/// </summary>
public static class Perspective
{
    public const int ViewHeight = 176;
    public const int CenterX = 120;
    public const int VanishY = 86;
    public const int FrontHalfWidth = 116;
    public const int FrontFloorY = 172;
    public const int FrontCeilingY = 4;

    /// <summary>Scale of the back wall relative to the front opening.</summary>
    public const float BackScale = 0.42f;

    private const float Depth = 1f / BackScale - 1f;

    /// <summary>Screen pixels per sprite pixel at the front of the room.</summary>
    public const float SpritePixel = 2.9f;

    public static float Scale(float z) => 1f / (1f + z * Depth);

    public static Vector2 Project(float x, float z, float h = 0)
    {
        float k = Scale(z);
        float floor = VanishY + (FrontFloorY - VanishY) * k;
        float wall = (FrontFloorY - FrontCeilingY) * k;
        return new Vector2(CenterX + x * FrontHalfWidth * k, floor - h * wall);
    }

    /// <summary>Screen height of the walls at depth z.</summary>
    public static float WallHeight(float z) => (FrontFloorY - FrontCeilingY) * Scale(z);

    /// <summary>Floor position under a screen point, or null when the point is not on the floor.</summary>
    public static (float x, float z)? ToFloor(float sx, float sy)
    {
        float k = (sy - VanishY) / (FrontFloorY - VanishY);
        if (k < BackScale - 0.02f || k > 1.05f)
            return null;
        k = Math.Clamp(k, BackScale, 1f);
        float z = (1f / k - 1f) / Depth;
        float x = (sx - CenterX) / (FrontHalfWidth * k);
        if (x < -1.05f || x > 1.05f)
            return null;
        return (Math.Clamp(x, -1f, 1f), Math.Clamp(z, 0f, 1f));
    }

    /// <summary>Screen rectangle of a sprite standing at (x, z), lifted h above the floor.</summary>
    public static RectF SpriteRect(int width, int height, float x, float z, float h = 0)
    {
        float s = SpritePixel * Scale(z);
        var foot = Project(x, z, h);
        float w = width * s, hh = height * s;
        return new RectF(foot.X - w / 2, foot.Y - hh, w, hh);
    }
}
