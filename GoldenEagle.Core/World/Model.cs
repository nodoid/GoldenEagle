using System.Collections.Generic;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Core.World;

/// <summary>The four walls of a room. Back is the far wall, Front the (unseen) near one.</summary>
public enum Side
{
    Back,
    Front,
    Left,
    Right,
}

public enum ExitKind
{
    Door,
    StairsUp,
    StairsDown,

    /// <summary>The castle gate: leaving through it with the eagle wins the game.</summary>
    Gate,
}

public enum ItemKind
{
    Key,
    Lamp,
    Cross,
    Potion,
    Bread,
    Eagle,

    // Treasures are scored when found and are not carried.
    Coins,
    Goblet,
    Crown,
    Jewels,
}

public enum MonsterKind
{
    Ghost,
    Bat,
    Knight,
}

public enum DecorKind
{
    Torch,
    Window,
    Shield,
    Tapestry,
    Books,
    Barrels,
    Altar,
    Throne,
    Bed,
    Chains,
    Cobweb,
    Bones,
    Cauldron,
    Table,
    Portrait,
}

/// <summary>
/// One end of a <see cref="Door"/> in a room. <see cref="Position"/> runs along the wall:
/// for Back / Front walls it is x (-1 left .. 1 right); for side walls it is depth z (0 near .. 1 far).
/// </summary>
public sealed record DoorEnd(string Room, Side Side, float Position, ExitKind Kind);

/// <summary>A passage between two rooms, optionally locked with a coloured key.</summary>
public sealed class Door
{
    public Door(int id, DoorEnd a, DoorEnd b, OricColor? lockColor)
    {
        Id = id;
        A = a;
        B = b;
        LockColor = lockColor;
    }

    public int Id { get; }
    public DoorEnd A { get; }
    public DoorEnd B { get; }

    /// <summary>Colour of the key that opens it, or null when it is never locked.</summary>
    public OricColor? LockColor { get; }

    public DoorEnd EndIn(string room) => A.Room == room ? A : B.Room == room ? B : null;

    public DoorEnd Other(string room) => A.Room == room ? B : A;
}

/// <summary>A trapdoor in the floor, x0..x1 by z0..z1. Falls into <see cref="Below"/>, or to death when null.</summary>
public sealed record Pit(float X0, float Z0, float X1, float Z1, string Below)
{
    public bool Contains(float x, float z) => x >= X0 && x <= X1 && z >= Z0 && z <= Z1;
}

/// <summary>Scenery painted on a wall (or standing on the floor for floor items).</summary>
public sealed record Decor(DecorKind Kind, Side Side, float Position);

public sealed record ItemPlacement(ItemKind Kind, OricColor Color, float X, float Z);

public sealed record MonsterPlacement(MonsterKind Kind, float X, float Z);

public enum Floor
{
    Cellar,
    Ground,
    Upper,
    Tower,
}

/// <summary>The fixed description of a room.</summary>
public sealed class RoomDef
{
    public RoomDef(string id, Loc name, Floor floor, int col, int row)
    {
        Id = id;
        Name = name;
        Floor = floor;
        Col = col;
        Row = row;
    }

    public string Id { get; }
    public Loc Name { get; }
    public Floor Floor { get; }
    public int Col { get; }
    public int Row { get; }

    public OricColor WallColor { get; init; } = OricColor.Blue;
    public OricColor LineColor { get; init; } = OricColor.Cyan;
    public OricColor FloorColor { get; init; } = OricColor.Red;
    public OricColor FloorLine { get; init; } = OricColor.Yellow;
    public bool Dark { get; init; }

    public List<Pit> Pits { get; } = new();
    public List<Decor> Decor { get; } = new();
    public List<ItemPlacement> Items { get; } = new();
    public List<MonsterPlacement> Monsters { get; } = new();
}

public static class ItemInfo
{
    public static bool IsTreasure(ItemKind kind) => kind >= ItemKind.Coins;

    public static int TreasureValue(ItemKind kind) => kind switch
    {
        ItemKind.Coins => 100,
        ItemKind.Goblet => 200,
        ItemKind.Jewels => 250,
        ItemKind.Crown => 300,
        _ => 0,
    };

    public static Loc ColorName(OricColor c) => c switch
    {
        OricColor.Red => new("ROUGE", "RED"),
        OricColor.Green => new("VERTE", "GREEN"),
        OricColor.Blue => new("BLEUE", "BLUE"),
        OricColor.Yellow => new("JAUNE", "YELLOW"),
        OricColor.Magenta => new("MAGENTA", "MAGENTA"),
        OricColor.Cyan => new("CYAN", "CYAN"),
        OricColor.White => new("BLANCHE", "WHITE"),
        _ => new("NOIRE", "BLACK"),
    };

    /// <summary>Name with its article, e.g. "LA CLÉ ROUGE" / "THE RED KEY".</summary>
    public static Loc Name(ItemKind kind, OricColor color) => kind switch
    {
        ItemKind.Key => new("LA CLÉ " + ColorName(color).Fr, "THE " + ColorName(color).En + " KEY"),
        ItemKind.Lamp => new("LA LAMPE", "THE LAMP"),
        ItemKind.Cross => new("LA CROIX", "THE CROSS"),
        ItemKind.Potion => new("UNE FIOLE " + ColorName(color).Fr, "A " + ColorName(color).En + " POTION"),
        ItemKind.Bread => new("UN PAIN", "A LOAF OF BREAD"),
        ItemKind.Eagle => new("L'AIGLE D'OR", "THE GOLDEN EAGLE"),
        ItemKind.Coins => new("DES PIÈCES D'OR", "GOLD COINS"),
        ItemKind.Goblet => new("UN CALICE", "A GOBLET"),
        ItemKind.Crown => new("UNE COURONNE", "A CROWN"),
        ItemKind.Jewels => new("DES BIJOUX", "JEWELS"),
        _ => new("?", "?"),
    };
}
