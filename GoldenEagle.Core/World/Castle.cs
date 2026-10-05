using System.Collections.Generic;
using System.Linq;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;

namespace GoldenEagle.Core.World;

/// <summary>
/// The castle of the Golden Eagle: cellars, ground floor, upper floor and the eyrie at the top
/// of the tower, linked by doors and stairs. Four coloured doors make the order of play:
/// red key (dungeon) - chapel and sacristy - green key - throne room and upper floor -
/// blue key - catacombs (dark: bring the lamp) - yellow key - tower and the eagle.
/// </summary>
public sealed class Castle
{
    public const string Start = "gatehouse";
    public const string Eyrie = "eyrie";

    private readonly Dictionary<string, RoomDef> _rooms = new();
    private readonly List<Door> _doors = new();

    public IReadOnlyCollection<RoomDef> Rooms => _rooms.Values;
    public IReadOnlyList<Door> Doors => _doors;

    public RoomDef this[string id] => _rooms[id];

    public bool Has(string id) => _rooms.ContainsKey(id);

    public IEnumerable<Door> DoorsOf(string room) => _doors.Where(d => d.A.Room == room || d.B.Room == room);

    private RoomDef Room(RoomDef r)
    {
        _rooms.Add(r.Id, r);
        return r;
    }

    private void Link(string a, Side sa, float pa, string b, Side sb, float pb, OricColor? lockColor = null,
        ExitKind kind = ExitKind.Door)
    {
        var ka = kind == ExitKind.StairsUp ? ExitKind.StairsUp : kind == ExitKind.StairsDown ? ExitKind.StairsDown : kind;
        var kb = kind == ExitKind.StairsUp ? ExitKind.StairsDown : kind == ExitKind.StairsDown ? ExitKind.StairsUp : kind;
        _doors.Add(new Door(_doors.Count, new DoorEnd(a, sa, pa, ka), new DoorEnd(b, sb, pb, kb), lockColor));
    }

    public static Castle Build()
    {
        var c = new Castle();

        // ---------------- ground floor ----------------
        var gate = c.Room(new RoomDef(Start, new("LE PORCHE", "THE GATEHOUSE"), Floor.Ground, 0, 0)
        {
            WallColor = OricColor.Blue, LineColor = OricColor.Cyan, FloorColor = OricColor.Green, FloorLine = OricColor.Black,
        });
        gate.Decor.Add(new Decor(DecorKind.Torch, Side.Left, 0.55f));
        gate.Decor.Add(new Decor(DecorKind.Torch, Side.Right, 0.55f));
        gate.Decor.Add(new Decor(DecorKind.Shield, Side.Back, 0.55f));
        gate.Items.Add(new ItemPlacement(ItemKind.Bread, OricColor.Yellow, -0.5f, 0.35f));

        var guard = c.Room(new RoomDef("guardroom", new("LA SALLE DES GARDES", "THE GUARD ROOM"), Floor.Ground, 1, 0)
        {
            WallColor = OricColor.Red, LineColor = OricColor.Yellow, FloorColor = OricColor.Black, FloorLine = OricColor.Red,
        });
        guard.Decor.Add(new Decor(DecorKind.Shield, Side.Back, -0.5f));
        guard.Decor.Add(new Decor(DecorKind.Shield, Side.Back, 0.5f));
        guard.Decor.Add(new Decor(DecorKind.Table, Side.Left, 0.5f));
        guard.Pits.Add(new Pit(0.25f, 0.30f, 0.65f, 0.48f, "oubliette"));
        guard.Monsters.Add(new MonsterPlacement(MonsterKind.Knight, 0f, 0.78f));
        guard.Items.Add(new ItemPlacement(ItemKind.Coins, OricColor.Yellow, -0.6f, 0.2f));

        var armoury = c.Room(new RoomDef("armoury", new("L'ARMURERIE", "THE ARMOURY"), Floor.Ground, 2, 0)
        {
            WallColor = OricColor.Black, LineColor = OricColor.White, FloorColor = OricColor.Blue, FloorLine = OricColor.Cyan,
        });
        armoury.Decor.Add(new Decor(DecorKind.Shield, Side.Left, 0.4f));
        armoury.Decor.Add(new Decor(DecorKind.Shield, Side.Left, 0.75f));
        armoury.Decor.Add(new Decor(DecorKind.Shield, Side.Right, 0.3f));
        armoury.Decor.Add(new Decor(DecorKind.Torch, Side.Back, -0.6f));
        armoury.Monsters.Add(new MonsterPlacement(MonsterKind.Knight, 0.3f, 0.55f));

        var hall = c.Room(new RoomDef("greathall", new("LA GRANDE SALLE", "THE GREAT HALL"), Floor.Ground, 0, 1)
        {
            WallColor = OricColor.Red, LineColor = OricColor.Yellow, FloorColor = OricColor.Yellow, FloorLine = OricColor.Red,
        });
        hall.Decor.Add(new Decor(DecorKind.Tapestry, Side.Back, -0.55f));
        hall.Decor.Add(new Decor(DecorKind.Tapestry, Side.Back, 0.55f));
        hall.Decor.Add(new Decor(DecorKind.Window, Side.Left, 0.5f));
        hall.Decor.Add(new Decor(DecorKind.Table, Side.Right, 0.6f));
        hall.Items.Add(new ItemPlacement(ItemKind.Goblet, OricColor.Yellow, 0.55f, 0.55f));

        var kitchen = c.Room(new RoomDef("kitchen", new("LES CUISINES", "THE KITCHENS"), Floor.Ground, 1, 1)
        {
            WallColor = OricColor.Yellow, LineColor = OricColor.Red, FloorColor = OricColor.Red, FloorLine = OricColor.Black,
        });
        kitchen.Decor.Add(new Decor(DecorKind.Cauldron, Side.Back, -0.3f));
        kitchen.Decor.Add(new Decor(DecorKind.Barrels, Side.Right, 0.7f));
        kitchen.Decor.Add(new Decor(DecorKind.Table, Side.Left, 0.3f));
        kitchen.Items.Add(new ItemPlacement(ItemKind.Bread, OricColor.Yellow, 0.2f, 0.4f));
        kitchen.Monsters.Add(new MonsterPlacement(MonsterKind.Bat, 0.4f, 0.6f));

        var chapel = c.Room(new RoomDef("chapel", new("LA CHAPELLE", "THE CHAPEL"), Floor.Ground, 2, 1)
        {
            WallColor = OricColor.White, LineColor = OricColor.Blue, FloorColor = OricColor.Blue, FloorLine = OricColor.White,
        });
        chapel.Decor.Add(new Decor(DecorKind.Altar, Side.Back, 0f));
        chapel.Decor.Add(new Decor(DecorKind.Window, Side.Left, 0.6f));
        chapel.Decor.Add(new Decor(DecorKind.Window, Side.Right, 0.6f));
        chapel.Items.Add(new ItemPlacement(ItemKind.Cross, OricColor.Yellow, 0f, 0.8f));
        chapel.Items.Add(new ItemPlacement(ItemKind.Potion, OricColor.Cyan, -0.6f, 0.3f));

        var library = c.Room(new RoomDef("library", new("LA BIBLIOTHÈQUE", "THE LIBRARY"), Floor.Ground, 0, 2)
        {
            WallColor = OricColor.Green, LineColor = OricColor.Yellow, FloorColor = OricColor.Red, FloorLine = OricColor.Yellow,
        });
        library.Decor.Add(new Decor(DecorKind.Books, Side.Back, -0.6f));
        library.Decor.Add(new Decor(DecorKind.Books, Side.Left, 0.35f));
        library.Decor.Add(new Decor(DecorKind.Books, Side.Left, 0.75f));
        library.Decor.Add(new Decor(DecorKind.Portrait, Side.Back, 0.15f));
        library.Items.Add(new ItemPlacement(ItemKind.Jewels, OricColor.Magenta, -0.3f, 0.5f));

        var throne = c.Room(new RoomDef("throne", new("LA SALLE DU TRÔNE", "THE THRONE ROOM"), Floor.Ground, 1, 2)
        {
            WallColor = OricColor.Magenta, LineColor = OricColor.Yellow, FloorColor = OricColor.Red, FloorLine = OricColor.Yellow,
        });
        throne.Decor.Add(new Decor(DecorKind.Throne, Side.Back, 0f));
        throne.Decor.Add(new Decor(DecorKind.Tapestry, Side.Left, 0.5f));
        throne.Decor.Add(new Decor(DecorKind.Torch, Side.Back, -0.45f));
        throne.Decor.Add(new Decor(DecorKind.Torch, Side.Back, 0.45f));
        throne.Items.Add(new ItemPlacement(ItemKind.Crown, OricColor.Yellow, 0f, 0.62f));
        throne.Monsters.Add(new MonsterPlacement(MonsterKind.Knight, -0.4f, 0.4f));

        var sacristy = c.Room(new RoomDef("sacristy", new("LA SACRISTIE", "THE SACRISTY"), Floor.Ground, 2, 2)
        {
            WallColor = OricColor.Cyan, LineColor = OricColor.Blue, FloorColor = OricColor.Magenta, FloorLine = OricColor.Black,
        });
        sacristy.Decor.Add(new Decor(DecorKind.Table, Side.Back, 0.3f));
        sacristy.Decor.Add(new Decor(DecorKind.Portrait, Side.Right, 0.5f));
        sacristy.Items.Add(new ItemPlacement(ItemKind.Key, OricColor.Green, 0.45f, 0.7f));

        // ---------------- cellars ----------------
        var wine = c.Room(new RoomDef("winecellar", new("LA CAVE À VIN", "THE WINE CELLAR"), Floor.Cellar, 1, 1)
        {
            WallColor = OricColor.Black, LineColor = OricColor.Yellow, FloorColor = OricColor.Black, FloorLine = OricColor.Red,
        });
        wine.Decor.Add(new Decor(DecorKind.Barrels, Side.Back, 0.4f));
        wine.Decor.Add(new Decor(DecorKind.Barrels, Side.Right, 0.4f));
        wine.Decor.Add(new Decor(DecorKind.Torch, Side.Left, 0.2f));
        wine.Items.Add(new ItemPlacement(ItemKind.Potion, OricColor.Magenta, 0.5f, 0.25f));

        var dungeon = c.Room(new RoomDef("dungeon", new("LE CACHOT", "THE DUNGEON"), Floor.Cellar, 0, 1)
        {
            WallColor = OricColor.Black, LineColor = OricColor.Green, FloorColor = OricColor.Black, FloorLine = OricColor.Green,
        });
        dungeon.Decor.Add(new Decor(DecorKind.Chains, Side.Back, -0.3f));
        dungeon.Decor.Add(new Decor(DecorKind.Chains, Side.Back, 0.4f));
        dungeon.Decor.Add(new Decor(DecorKind.Bones, Side.Left, 0.6f));
        dungeon.Decor.Add(new Decor(DecorKind.Torch, Side.Right, 0.5f));
        dungeon.Items.Add(new ItemPlacement(ItemKind.Key, OricColor.Red, -0.55f, 0.75f));
        dungeon.Monsters.Add(new MonsterPlacement(MonsterKind.Bat, -0.2f, 0.5f));

        var crypt = c.Room(new RoomDef("crypt", new("LA CRYPTE", "THE CRYPT"), Floor.Cellar, 1, 2)
        {
            WallColor = OricColor.Black, LineColor = OricColor.Magenta, FloorColor = OricColor.Blue, FloorLine = OricColor.Black,
        });
        crypt.Decor.Add(new Decor(DecorKind.Altar, Side.Back, -0.35f));
        crypt.Decor.Add(new Decor(DecorKind.Cobweb, Side.Left, 0.8f));
        crypt.Decor.Add(new Decor(DecorKind.Torch, Side.Back, 0.5f));
        crypt.Items.Add(new ItemPlacement(ItemKind.Coins, OricColor.Yellow, 0.6f, 0.65f));
        crypt.Monsters.Add(new MonsterPlacement(MonsterKind.Ghost, -0.4f, 0.7f));

        var catacombs = c.Room(new RoomDef("catacombs", new("LES CATACOMBES", "THE CATACOMBS"), Floor.Cellar, 2, 2)
        {
            WallColor = OricColor.Black, LineColor = OricColor.White, FloorColor = OricColor.Black, FloorLine = OricColor.Blue,
            Dark = true,
        });
        catacombs.Decor.Add(new Decor(DecorKind.Bones, Side.Back, -0.5f));
        catacombs.Decor.Add(new Decor(DecorKind.Bones, Side.Right, 0.4f));
        catacombs.Decor.Add(new Decor(DecorKind.Cobweb, Side.Back, 0.6f));
        catacombs.Pits.Add(new Pit(-0.35f, 0.38f, 0.15f, 0.62f, null));
        catacombs.Items.Add(new ItemPlacement(ItemKind.Key, OricColor.Yellow, 0.5f, 0.8f));
        catacombs.Monsters.Add(new MonsterPlacement(MonsterKind.Ghost, 0.6f, 0.4f));

        var oubliette = c.Room(new RoomDef("oubliette", new("L'OUBLIETTE", "THE OUBLIETTE"), Floor.Cellar, 1, 0)
        {
            WallColor = OricColor.Black, LineColor = OricColor.Red, FloorColor = OricColor.Black, FloorLine = OricColor.Red,
            Dark = true,
        });
        oubliette.Decor.Add(new Decor(DecorKind.Bones, Side.Back, 0f));
        oubliette.Decor.Add(new Decor(DecorKind.Chains, Side.Left, 0.5f));
        oubliette.Items.Add(new ItemPlacement(ItemKind.Jewels, OricColor.Cyan, -0.5f, 0.55f));

        // ---------------- upper floor ----------------
        var gallery = c.Room(new RoomDef("gallery", new("LA GALERIE", "THE GALLERY"), Floor.Upper, 1, 2)
        {
            WallColor = OricColor.Blue, LineColor = OricColor.White, FloorColor = OricColor.Magenta, FloorLine = OricColor.White,
        });
        gallery.Decor.Add(new Decor(DecorKind.Portrait, Side.Back, -0.5f));
        gallery.Decor.Add(new Decor(DecorKind.Portrait, Side.Left, 0.4f));
        gallery.Decor.Add(new Decor(DecorKind.Portrait, Side.Left, 0.75f));
        gallery.Decor.Add(new Decor(DecorKind.Window, Side.Right, 0.35f));
        gallery.Monsters.Add(new MonsterPlacement(MonsterKind.Knight, 0.2f, 0.6f));

        var bedroom = c.Room(new RoomDef("bedroom", new("LA CHAMBRE DU SEIGNEUR", "THE LORD'S BEDCHAMBER"), Floor.Upper, 2, 2)
        {
            WallColor = OricColor.Magenta, LineColor = OricColor.White, FloorColor = OricColor.Blue, FloorLine = OricColor.Magenta,
        });
        bedroom.Decor.Add(new Decor(DecorKind.Bed, Side.Back, 0.3f));
        bedroom.Decor.Add(new Decor(DecorKind.Window, Side.Right, 0.6f));
        bedroom.Decor.Add(new Decor(DecorKind.Tapestry, Side.Left, 0.5f));
        bedroom.Items.Add(new ItemPlacement(ItemKind.Key, OricColor.Blue, 0.55f, 0.35f));
        bedroom.Items.Add(new ItemPlacement(ItemKind.Bread, OricColor.Yellow, -0.4f, 0.75f));

        var lab = c.Room(new RoomDef("laboratory", new("LE LABORATOIRE", "THE LABORATORY"), Floor.Upper, 1, 3)
        {
            WallColor = OricColor.Green, LineColor = OricColor.White, FloorColor = OricColor.Black, FloorLine = OricColor.Green,
        });
        lab.Decor.Add(new Decor(DecorKind.Cauldron, Side.Back, 0.2f));
        lab.Decor.Add(new Decor(DecorKind.Books, Side.Right, 0.5f));
        lab.Decor.Add(new Decor(DecorKind.Table, Side.Left, 0.6f));
        lab.Items.Add(new ItemPlacement(ItemKind.Lamp, OricColor.Yellow, -0.5f, 0.6f));
        lab.Items.Add(new ItemPlacement(ItemKind.Potion, OricColor.Green, 0.45f, 0.4f));
        lab.Monsters.Add(new MonsterPlacement(MonsterKind.Bat, 0f, 0.5f));

        var ante = c.Room(new RoomDef("antechamber", new("L'ANTICHAMBRE", "THE ANTECHAMBER"), Floor.Upper, 2, 3)
        {
            WallColor = OricColor.Cyan, LineColor = OricColor.Black, FloorColor = OricColor.Red, FloorLine = OricColor.Yellow,
        });
        ante.Decor.Add(new Decor(DecorKind.Shield, Side.Back, -0.4f));
        ante.Decor.Add(new Decor(DecorKind.Torch, Side.Left, 0.5f));
        ante.Pits.Add(new Pit(-0.3f, 0.45f, 0.1f, 0.65f, "throne"));

        var stair = c.Room(new RoomDef("towerstair", new("L'ESCALIER DE LA TOUR", "THE TOWER STAIRS"), Floor.Upper, 3, 3)
        {
            WallColor = OricColor.Yellow, LineColor = OricColor.Black, FloorColor = OricColor.Black, FloorLine = OricColor.Yellow,
        });
        stair.Decor.Add(new Decor(DecorKind.Window, Side.Right, 0.5f));
        stair.Decor.Add(new Decor(DecorKind.Cobweb, Side.Left, 0.8f));
        stair.Monsters.Add(new MonsterPlacement(MonsterKind.Bat, 0.2f, 0.4f));

        var eyrie = c.Room(new RoomDef(Eyrie, new("L'AIRE DE L'AIGLE", "THE EAGLE'S EYRIE"), Floor.Tower, 3, 3)
        {
            WallColor = OricColor.Blue, LineColor = OricColor.Yellow, FloorColor = OricColor.Cyan, FloorLine = OricColor.Blue,
        });
        eyrie.Decor.Add(new Decor(DecorKind.Window, Side.Left, 0.5f));
        eyrie.Decor.Add(new Decor(DecorKind.Window, Side.Right, 0.5f));
        eyrie.Decor.Add(new Decor(DecorKind.Window, Side.Back, -0.55f));
        eyrie.Decor.Add(new Decor(DecorKind.Window, Side.Back, 0.55f));
        eyrie.Items.Add(new ItemPlacement(ItemKind.Eagle, OricColor.Yellow, 0f, 0.75f));
        eyrie.Monsters.Add(new MonsterPlacement(MonsterKind.Ghost, -0.6f, 0.6f));
        eyrie.Monsters.Add(new MonsterPlacement(MonsterKind.Ghost, 0.6f, 0.6f));

        // ---------------- doors and stairs ----------------
        c._doors.Add(new Door(c._doors.Count, new DoorEnd(Start, Side.Front, 0f, ExitKind.Gate),
            new DoorEnd(Start, Side.Front, 0f, ExitKind.Gate), null));
        c.Link(Start, Side.Back, 0f, "greathall", Side.Front, 0f);
        c.Link(Start, Side.Right, 0.5f, "guardroom", Side.Left, 0.5f);
        c.Link("guardroom", Side.Right, 0.6f, "armoury", Side.Left, 0.6f);
        c.Link("guardroom", Side.Back, -0.55f, "kitchen", Side.Front, -0.55f);
        c.Link("greathall", Side.Right, 0.3f, "kitchen", Side.Left, 0.3f);
        c.Link("greathall", Side.Back, 0f, "library", Side.Front, 0f);
        c.Link("armoury", Side.Back, 0.5f, "chapel", Side.Front, 0.5f, OricColor.Red);
        c.Link("chapel", Side.Back, 0.6f, "sacristy", Side.Front, 0.6f);
        c.Link("library", Side.Right, 0.5f, "throne", Side.Left, 0.5f, OricColor.Green);
        c.Link("kitchen", Side.Back, 0.55f, "winecellar", Side.Back, -0.55f, kind: ExitKind.StairsDown);
        c.Link("throne", Side.Right, 0.7f, "gallery", Side.Left, 0.7f, kind: ExitKind.StairsUp);

        c.Link("winecellar", Side.Left, 0.5f, "dungeon", Side.Right, 0.5f);
        c.Link("winecellar", Side.Right, 0.7f, "crypt", Side.Left, 0.3f);
        c.Link("oubliette", Side.Back, 0.4f, "winecellar", Side.Front, 0.4f);
        c.Link("crypt", Side.Back, 0.1f, "catacombs", Side.Front, 0.1f, OricColor.Blue);

        c.Link("gallery", Side.Right, 0.4f, "bedroom", Side.Left, 0.4f);
        c.Link("gallery", Side.Back, 0f, "laboratory", Side.Front, 0f);
        c.Link("laboratory", Side.Right, 0.5f, "antechamber", Side.Left, 0.5f);
        c.Link("bedroom", Side.Back, -0.4f, "antechamber", Side.Front, -0.4f);
        c.Link("antechamber", Side.Right, 0.4f, "towerstair", Side.Left, 0.4f, OricColor.Yellow);
        c.Link("towerstair", Side.Back, 0f, Eyrie, Side.Front, 0f, kind: ExitKind.StairsUp);
        return c;
    }

    /// <summary>The gate out of the castle (in the gatehouse's front wall).</summary>
    public Door Gate => _doors[0];
}
