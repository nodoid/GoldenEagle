using Microsoft.Xna.Framework;

namespace GoldenEagle.Core.Engine;

/// <summary>Layout of the panel under the room picture (shared by drawing and touch hit tests).</summary>
public static class Hud
{
    public const int Top = Perspective.ViewHeight;
    public const int MessageY = Top + 2;
    public const int SlotY = Top + 12;
    public const int SlotSize = 24;
    public const int SlotGap = 4;
    public const int SlotX = 3;
    public const int ButtonX = 118;
    public const int ButtonWidth = 64;
    public const int ButtonHeight = 11;
    public const int UseY = SlotY;
    public const int DropY = SlotY + 13;
    public const int StatusY = 214;

    public static Rectangle Slot(int i) => new(SlotX + i * (SlotSize + SlotGap), SlotY, SlotSize, SlotSize);

    public static Rectangle UseButton => new(ButtonX, UseY, ButtonWidth, ButtonHeight);

    public static Rectangle DropButton => new(ButtonX, DropY, ButtonWidth, ButtonHeight);

    public static bool Hit(Rectangle r, Vector2 p) => p.X >= r.Left && p.X < r.Right && p.Y >= r.Top && p.Y < r.Bottom;
}
