using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;
using GoldenEagle.Core.World;

namespace GoldenEagle.Core.Engine;

/// <summary>
/// Draws a room in perspective, as in the original but at high resolution: shaded stone walls
/// and flagstones, arched doorways, flickering torch light, soft shadows, then the objects,
/// monsters and the hero, and the panel below.
/// </summary>
public static class RoomRenderer
{
    private const float DoorHeight = 0.62f;
    private const float ArchSegments = 10;

    private static readonly Color Torchlight = new(255, 150, 40);
    private static readonly Color Daylight = new(120, 190, 255);

    public static void Draw(Canvas c, Adventure a, int tick, bool mobile)
    {
        DrawRoom(c, a, tick);
        DrawHud(c, a, tick, mobile);
    }

    /// <summary>The rich colour used for large surfaces painted in an Oric colour.</summary>
    public static Color Surface(OricColor c) => c switch
    {
        OricColor.Black => new Color(58, 55, 66),
        OricColor.Red => new Color(170, 40, 34),
        OricColor.Green => new Color(40, 130, 62),
        OricColor.Yellow => new Color(214, 168, 48),
        OricColor.Blue => new Color(44, 64, 168),
        OricColor.Magenta => new Color(138, 44, 136),
        OricColor.Cyan => new Color(42, 148, 158),
        _ => new Color(206, 202, 188),
    };

    private static Vector2 P(float x, float z, float h = 0) => Perspective.Project(x, z, h);

    /// <summary>Depth of a point on the floor from its screen row.</summary>
    private static float DepthAtRow(float y)
    {
        float k = Math.Clamp((y - Perspective.VanishY) / (Perspective.FrontFloorY - Perspective.VanishY), Perspective.BackScale, 1f);
        return (1f / k - 1f) / (1f / Perspective.BackScale - 1f);
    }

    /// <summary>Depth of a point on a side wall from its screen column.</summary>
    private static float DepthAtColumn(float x)
    {
        float k = Math.Clamp(Math.Abs(x - Perspective.CenterX) / Perspective.FrontHalfWidth, Perspective.BackScale, 1f);
        return (1f / k - 1f) / (1f / Perspective.BackScale - 1f);
    }

    public static void DrawRoom(Canvas c, Adventure a, int tick)
    {
        var room = a.Room;
        bool lit = a.CanSee(room.Id);
        bool lamp = room.Dark && lit;
        c.Fill(0, 0, Canvas.Width, Perspective.ViewHeight, Color.Black);

        // In a dark room the lamp makes a pool of light around the hero.
        var hero = P(a.X, a.Z, 0.25f);
        Func<float, float, float> light = lamp
            ? (x, y) => Math.Clamp(1.15f - Vector2.Distance(new Vector2(x, y), hero) / 140f, 0.06f, 1f)
            : (_, _) => 1f;

        if (lit)
        {
            DrawSurfaces(c, room, light, tick);
        }
        else
        {
            DrawEdges(c, new Color(30, 40, 110));
        }

        if (lit)
            foreach (var d in room.Decor.Where(d => d.Side != Side.Front))
                DrawDecor(c, d, tick);

        foreach (var door in a.Castle.DoorsOf(room.Id))
            DrawDoor(c, a, door, door.EndIn(room.Id), lit, room);

        if (lit)
            foreach (var pit in room.Pits)
                DrawPit(c, pit, room, tick);

        // Everything standing in the room, far to near.
        var things = new List<(float z, Action draw)>();
        if (lit)
            foreach (var item in a.ItemsIn(room.Id))
                things.Add((item.Z, () => DrawItem(c, item, tick)));
        foreach (var m in a.MonstersIn(room.Id))
            things.Add((m.Z, () => DrawMonster(c, m, tick, lit)));
        things.Add((a.Z, () => DrawHero(c, a, tick)));
        foreach (var t in things.OrderByDescending(t => t.z))
            t.draw();

        if (lit && !lamp)
            DrawTorchLight(c, room, tick);
    }

    // ---------------------------------------------------------------- walls and floor

    private static void DrawSurfaces(Canvas c, RoomDef room, Func<float, float, float> light, int tick)
    {
        var wall = Surface(room.WallColor);
        var floor = Surface(room.FloorColor);

        // Ceiling: the wall colour in deep shadow.
        c.ShadePolygon([P(-1, 0, 1), P(1, 0, 1), P(1, 1, 1), P(-1, 1, 1)], wall,
            (x, y) => (0.12f + 0.12f * y / Perspective.VanishY) * light(x, y));
        // Back wall, lit a little more towards the floor.
        var top = P(-1, 1, 1).Y;
        var bottom = P(-1, 1, 0).Y;
        c.ShadePolygon([P(-1, 1, 1), P(1, 1, 1), P(1, 1, 0), P(-1, 1, 0)], wall,
            (x, y) => (0.55f + 0.2f * (y - top) / (bottom - top)) * light(x, y));
        // Side walls fade into the distance.
        c.ShadePolygon([P(-1, 0, 1), P(-1, 1, 1), P(-1, 1, 0), P(-1, 0, 0)], wall,
            (x, y) => (0.95f - 0.4f * DepthAtColumn(x)) * light(x, y));
        c.ShadePolygon([P(1, 1, 1), P(1, 0, 1), P(1, 0, 0), P(1, 1, 0)], wall,
            (x, y) => (0.95f - 0.4f * DepthAtColumn(x)) * light(x, y));
        // Floor.
        c.ShadePolygon([P(-1, 1, 0), P(1, 1, 0), P(1, 0, 0), P(-1, 0, 0)], floor,
            (x, y) => (1f - 0.5f * DepthAtRow(y)) * light(x, y));

        DrawStonework(c, room, wall);
        DrawFlagstones(c, room, floor);
        DrawEdges(c, room.LineColor.ToColor() * 0.7f);
    }

    private static Color Darker(Color c, float f) => new((int)(c.R * f), (int)(c.G * f), (int)(c.B * f));

    private static void DrawStonework(Canvas c, RoomDef room, Color wall)
    {
        var mortar = Darker(wall, 0.45f) * 0.8f;
        const int courses = 9;
        for (int i = 1; i < courses; i++)
        {
            float h = i / (float)courses;
            // Back wall course and its staggered joints.
            c.Line(P(-1, 1, h), P(1, 1, h), mortar, 0.35f);
            float offset = i % 2 == 0 ? 0f : 0.0625f;
            for (float x = -1f + 0.125f + offset; x < 1f; x += 0.125f)
                c.Line(P(x, 1, h), P(x, 1, h - 1f / courses), mortar, 0.3f);
            // Side wall courses run to the vanishing point.
            c.Line(P(-1, 0, h), P(-1, 1, h), mortar, 0.35f);
            c.Line(P(1, 0, h), P(1, 1, h), mortar, 0.35f);
            for (float z = 0.08f + offset * 1.6f; z < 1f; z += 0.2f)
            {
                c.Line(P(-1, z, h), P(-1, z, h - 1f / courses), mortar, 0.3f);
                c.Line(P(1, z, h), P(1, z, h - 1f / courses), mortar, 0.3f);
            }
        }
    }

    private static void DrawFlagstones(Canvas c, RoomDef room, Color floor)
    {
        var line = room.FloorLine == room.FloorColor ? Darker(floor, 0.5f) : Surface(room.FloorLine) * 0.6f;
        for (float z = 0.125f; z < 1f; z += 0.25f)
            c.Line(P(-1, z), P(1, z), line, 0.4f);
        for (float x = -0.75f; x < 1f; x += 0.25f)
            c.Line(P(x, 0), P(x, 1), line, 0.4f);
    }

    private static void DrawEdges(Canvas c, Color line)
    {
        var bl = P(-1, 1, 0);
        var br = P(1, 1, 0);
        var tl = P(-1, 1, 1);
        var tr = P(1, 1, 1);
        c.Line(tl, tr, line, 0.8f);
        c.Line(bl, br, line, 0.8f);
        c.Line(tl, bl, line, 0.8f);
        c.Line(tr, br, line, 0.8f);
        c.Line(P(-1, 0, 0), bl, line, 0.8f);
        c.Line(P(1, 0, 0), br, line, 0.8f);
        c.Line(P(-1, 0, 1), tl, line, 0.8f);
        c.Line(P(1, 0, 1), tr, line, 0.8f);
    }

    private static void DrawTorchLight(Canvas c, RoomDef room, int tick)
    {
        foreach (var d in room.Decor)
        {
            if (d.Kind == DecorKind.Torch)
            {
                var r = DecorRect(d, tick);
                float flicker = 0.85f + 0.15f * MathF.Sin(tick * 0.37f + d.Position * 10);
                c.Glow(r.X + r.W / 2, r.Y + r.H * 0.15f, 9f + r.H * 1.4f, Torchlight * (0.28f * flicker));
            }
            else if (d.Kind == DecorKind.Window)
            {
                var r = DecorRect(d, tick);
                c.Glow(r.X + r.W / 2, r.Y + r.H / 2, r.H * 1.1f, Daylight * 0.12f);
            }
        }
    }

    // ---------------------------------------------------------------- doors

    /// <summary>A point on the plane of a wall: along = x on back/front walls, z on side walls.</summary>
    private static Vector2 OnWall(Side side, float along, float h) => side switch
    {
        Side.Back => P(along, 1, h),
        Side.Left => P(-1, along, h),
        Side.Right => P(1, along, h),
        _ => P(along, 0, h),
    };

    /// <summary>Outline of an arched doorway in screen space (front exits are a mat on the floor).</summary>
    public static Vector2[] DoorShape(DoorEnd end)
    {
        float p = end.Position;
        if (end.Side == Side.Front)
        {
            return
            [
                P(p - Adventure.DoorHalfWidth, 0.07f), P(p + Adventure.DoorHalfWidth, 0.07f),
                P(p + Adventure.DoorHalfWidth + 0.03f, 0), P(p - Adventure.DoorHalfWidth - 0.03f, 0),
            ];
        }
        float half = end.Side == Side.Back ? Adventure.DoorHalfWidth : Adventure.SideDoorHalfDepth;
        // The arch is a half circle in the wall's own plane (its radius in wall-height units).
        float radius = end.Side == Side.Back ? 0.12f : 0.12f;
        float spring = DoorHeight - radius;
        var pts = new List<Vector2> { OnWall(end.Side, p - half, 0), OnWall(end.Side, p - half, spring) };
        for (int i = 1; i < ArchSegments; i++)
        {
            float t = MathF.PI * i / ArchSegments;
            pts.Add(OnWall(end.Side, p - half * MathF.Cos(t), spring + radius * MathF.Sin(t)));
        }
        pts.Add(OnWall(end.Side, p + half, spring));
        pts.Add(OnWall(end.Side, p + half, 0));
        return pts.ToArray();
    }

    /// <summary>Bounding box of a door on screen, enlarged a little for tapping.</summary>
    public static RectF DoorBounds(DoorEnd end)
    {
        var q = DoorShape(end);
        float x0 = q.Min(p => p.X), x1 = q.Max(p => p.X), y0 = q.Min(p => p.Y), y1 = q.Max(p => p.Y);
        var r = new RectF(x0 - 3, y0 - 3, x1 - x0 + 6, y1 - y0 + 6);
        if (end.Side == Side.Front)
            r = new RectF(r.X, r.Y - 4, r.W, Perspective.ViewHeight - (r.Y - 4));
        return r;
    }

    private static void DrawDoor(Canvas c, Adventure a, Door door, DoorEnd end, bool lit, RoomDef room)
    {
        var shape = DoorShape(end);
        bool locked = !a.IsUnlocked(door);
        var frame = lit ? room.LineColor.ToColor() : new Color(30, 40, 110);
        if (end.Side == Side.Front)
        {
            // The front wall is behind the viewer: its exits are marked on the floor.
            var mat = end.Kind == ExitKind.Gate ? Surface(OricColor.Yellow) : locked ? Surface(door.LockColor!.Value) : new Color(20, 18, 24);
            if (lit || end.Kind == ExitKind.Gate)
                c.FillPolygon(shape, mat);
            c.Outline(shape, frame, 0.6f);
            float cx = (shape[0].X + shape[1].X) / 2, y = shape[0].Y + 1;
            var arrow = end.Kind == ExitKind.Gate ? OricColor.Red.ToColor() : Color.White;
            c.Line(new Vector2(cx - 3, y), new Vector2(cx, y + 3), arrow, 0.8f);
            c.Line(new Vector2(cx + 3, y), new Vector2(cx, y + 3), arrow, 0.8f);
            if (end.Kind is ExitKind.StairsUp or ExitKind.StairsDown)
                DrawStairMark(c, end.Kind, cx, y - 9, Color.White);
            return;
        }

        if (locked && lit)
        {
            // A stout door in the colour of its key.
            var wood = Surface(door.LockColor!.Value);
            c.ShadePolygon(shape, wood, (x, y) => 0.75f + 0.25f * MathF.Sin(x * 1.3f));
            var b0 = OnWall(end.Side, end.Position, 0);
            for (int i = -2; i <= 2; i++)
            {
                float along = end.Position + i * (end.Side == Side.Back ? 0.06f : 0.032f);
                c.Line(OnWall(end.Side, along, 0), OnWall(end.Side, along, DoorHeight - 0.02f), Darker(wood, 0.5f), 0.35f);
            }
            float half = end.Side == Side.Back ? Adventure.DoorHalfWidth : Adventure.SideDoorHalfDepth;
            foreach (float h in new[] { 0.12f, 0.4f })
                c.Line(OnWall(end.Side, end.Position - half, h), OnWall(end.Side, end.Position + half, h), new Color(60, 60, 70), 0.9f);
            var hole = OnWall(end.Side, end.Position + half * 0.55f, 0.27f);
            float s = Perspective.Scale(end.Side == Side.Back ? 1 : end.Position);
            c.Fill(hole.X - 1.2f * s, hole.Y - 1.6f * s, 2.4f * s, 2.4f * s, Color.Gold);
            c.Fill(hole.X - 0.4f * s, hole.Y - 0.8f * s, 0.8f * s, 2.6f * s, Color.Black);
            _ = b0;
        }
        else
        {
            // An open archway into darkness.
            var mid = OnWall(end.Side, end.Position, 0.3f);
            c.ShadePolygon(shape, lit ? new Color(40, 34, 30) : Color.Black,
                (x, y) => 0.25f + 0.5f * Math.Clamp((y - mid.Y) / 40f + 0.5f, 0f, 1f));
            if (end.Kind is ExitKind.StairsUp or ExitKind.StairsDown && lit)
                DrawSteps(c, end, end.Kind == ExitKind.StairsUp);
        }
        c.Outline(shape, frame, lit ? 1.1f : 0.6f);
        if (end.Kind is ExitKind.StairsUp or ExitKind.StairsDown)
        {
            var topMid = OnWall(end.Side, end.Position, DoorHeight + 0.04f);
            DrawStairMark(c, end.Kind, topMid.X, topMid.Y - 8, Color.White);
        }
    }

    private static void DrawSteps(Canvas c, DoorEnd end, bool up)
    {
        float half = (end.Side == Side.Back ? Adventure.DoorHalfWidth : Adventure.SideDoorHalfDepth) * 0.9f;
        const int steps = 6;
        for (int i = 0; i < steps; i++)
        {
            float h0 = up ? i * 0.07f : (steps - 1 - i) * 0.05f;
            float inset = up ? 0f : i * half * 0.08f;
            var a = OnWall(end.Side, end.Position - half + inset, h0);
            var b = OnWall(end.Side, end.Position + half - inset, h0);
            var a2 = OnWall(end.Side, end.Position - half + inset, h0 + 0.045f);
            var b2 = OnWall(end.Side, end.Position + half - inset, h0 + 0.045f);
            float shade = up ? 0.9f - i * 0.1f : 0.35f + i * 0.08f;
            c.FillPolygon([a2, b2, b, a], new Color(150, 145, 140) * shade);
            c.Line(a2, b2, new Color(220, 215, 205) * shade, 0.4f);
        }
    }

    private static void DrawStairMark(Canvas c, ExitKind kind, float x, float y, Color color)
    {
        if (kind == ExitKind.StairsUp)
        {
            c.Line(new Vector2(x, y), new Vector2(x, y + 6), color, 0.8f);
            c.Line(new Vector2(x - 3, y + 3), new Vector2(x, y), color, 0.8f);
            c.Line(new Vector2(x + 3, y + 3), new Vector2(x, y), color, 0.8f);
        }
        else if (kind == ExitKind.StairsDown)
        {
            c.Line(new Vector2(x, y), new Vector2(x, y + 6), color, 0.8f);
            c.Line(new Vector2(x - 3, y + 3), new Vector2(x, y + 6), color, 0.8f);
            c.Line(new Vector2(x + 3, y + 3), new Vector2(x, y + 6), color, 0.8f);
        }
    }

    private static void DrawPit(Canvas c, Pit pit, RoomDef room, int tick)
    {
        Vector2[] q = [P(pit.X0, pit.Z1), P(pit.X1, pit.Z1), P(pit.X1, pit.Z0), P(pit.X0, pit.Z0)];
        c.FillPolygon(q, Color.Black);
        // The near inside edge of the shaft, then the rim.
        float drop = 0.07f;
        Vector2[] inner = [P(pit.X0, pit.Z1), P(pit.X1, pit.Z1), P(pit.X1, pit.Z1 - drop), P(pit.X0, pit.Z1 - drop)];
        c.ShadePolygon(inner, Surface(room.FloorColor), (x, y) => 0.35f - 0.25f * (y - inner[0].Y) / MathF.Max(1, inner[2].Y - inner[0].Y));
        c.Outline(q, room.LineColor.ToColor() * 0.9f, 0.7f);
    }

    // ---------------------------------------------------------------- scenery

    private static (string[] art, float width, float h0, float h1) DecorArt(DecorKind kind, int tick) => kind switch
    {
        DecorKind.Torch => (tick / 6 % 2 == 0 ? SpriteData.Torch : SpriteData.TorchFlicker, 0.07f, 0.36f, 0.62f),
        DecorKind.Window => (SpriteData.Window, 0.26f, 0.38f, 0.82f),
        DecorKind.Shield => (SpriteData.Shield, 0.17f, 0.42f, 0.66f),
        DecorKind.Tapestry => (SpriteData.Tapestry, 0.28f, 0.3f, 0.86f),
        DecorKind.Books => (SpriteData.Books, 0.44f, 0f, 0.62f),
        DecorKind.Barrels => (SpriteData.Barrels, 0.42f, 0f, 0.3f),
        DecorKind.Altar => (SpriteData.Altar, 0.44f, 0f, 0.34f),
        DecorKind.Throne => (SpriteData.Throne, 0.3f, 0f, 0.5f),
        DecorKind.Bed => (SpriteData.Bed, 0.6f, 0f, 0.28f),
        DecorKind.Chains => (SpriteData.Chains, 0.08f, 0.38f, 0.82f),
        DecorKind.Cobweb => (SpriteData.Cobweb, 0.3f, 0.7f, 1f),
        DecorKind.Bones => (SpriteData.Bones, 0.3f, 0f, 0.1f),
        DecorKind.Cauldron => (SpriteData.Cauldron, 0.32f, 0f, 0.3f),
        DecorKind.Table => (SpriteData.Table, 0.42f, 0f, 0.22f),
        _ => (SpriteData.Portrait, 0.22f, 0.42f, 0.74f),
    };

    private static RectF DecorRect(Decor d, int tick)
    {
        var (_, width, h0, h1) = DecorArt(d.Kind, tick);
        if (d.Side == Side.Back)
        {
            var a = P(d.Position - width / 2, 1, h1);
            var b = P(d.Position + width / 2, 1, h0);
            return new RectF(a.X, a.Y, b.X - a.X, b.Y - a.Y);
        }
        // On a side wall the picture stands in its perspective box, keeping a believable width.
        float x = d.Side == Side.Left ? -1 : 1;
        float half = width * 0.5f;
        var top = P(x, d.Position, h1);
        var bottom = P(x, d.Position, h0);
        var near = P(x, d.Position - half, h0);
        var far = P(x, d.Position + half, h0);
        float h = Math.Max(1, bottom.Y - top.Y);
        var art = DecorArt(d.Kind, tick).art;
        float aspect = art.Max(r => r.Length) / (float)art.Length;
        float w = Math.Max(Math.Abs(near.X - far.X), h * aspect * 0.55f);
        return new RectF(top.X - w / 2, top.Y, w, h);
    }

    private static void DrawDecor(Canvas c, Decor d, int tick)
    {
        var (art, _, _, _) = DecorArt(d.Kind, tick);
        var sprite = Sprite.Of(art, d.Kind.ToString());
        bool mirror = d.Side == Side.Right && d.Kind != DecorKind.Cobweb || d.Side == Side.Left && d.Kind == DecorKind.Cobweb && false;
        if (d.Kind == DecorKind.Cobweb)
            mirror = d.Side == Side.Right || (d.Side == Side.Back && d.Position > 0);
        var r = DecorRect(d, tick);
        if (d.Kind is not (DecorKind.Cobweb or DecorKind.Window or DecorKind.Torch or DecorKind.Chains))
            c.Shadow(r.X + r.W / 2 + 1, r.Bottom, r.W * 1.05f, Math.Max(2, r.H * 0.12f), 0.35f);
        c.DrawScaled(sprite, r, mirror, d.Kind == DecorKind.Cobweb ? Color.White * 0.7f : null);
    }

    // ---------------------------------------------------------------- figures

    public static string[] ItemArt(ItemKind kind) => kind switch
    {
        ItemKind.Key => SpriteData.Key,
        ItemKind.Lamp => SpriteData.Lamp,
        ItemKind.Cross => SpriteData.Cross,
        ItemKind.Potion => SpriteData.Potion,
        ItemKind.Bread => SpriteData.Bread,
        ItemKind.Eagle => SpriteData.Eagle,
        ItemKind.Coins => SpriteData.Coins,
        ItemKind.Goblet => SpriteData.Goblet,
        ItemKind.Crown => SpriteData.Crown,
        _ => SpriteData.Jewels,
    };

    /// <summary>The sprite of an object in its own colour (keys tinted, potion liquid and gems recoloured).</summary>
    public static Sprite ItemSprite(ItemKind kind, OricColor color)
    {
        var sprite = Sprite.Of(ItemArt(kind), kind.ToString());
        return kind switch
        {
            ItemKind.Key => sprite.Recolor(color),
            ItemKind.Potion or ItemKind.Jewels => Recolored(sprite, OricColor.Red, color),
            _ => sprite,
        };
    }

    private static readonly Dictionary<(Sprite, OricColor, OricColor), Sprite> Swaps = new();

    private static Sprite Recolored(Sprite s, OricColor from, OricColor to)
    {
        if (from == to)
            return s;
        lock (Swaps)
        {
            if (Swaps.TryGetValue((s, from, to), out var r))
                return r;
            var rows = new string[s.Height];
            for (int y = 0; y < s.Height; y++)
            {
                var chars = new char[s.Width];
                for (int x = 0; x < s.Width; x++)
                {
                    var col = s.ColorAt(x, y);
                    chars[x] = col is OricColor k ? OricPalette.ToCode(k == from ? to : k) : '.';
                }
                rows[y] = new string(chars);
            }
            r = new Sprite(s.Name + "-" + to, rows);
            Swaps[(s, from, to)] = r;
            return r;
        }
    }

    /// <summary>Objects lie on the floor at the size they would have in the hero's hand.</summary>
    private const float ItemPixel = 0.75f;

    public static RectF ItemRect(ItemInstance item)
    {
        var art = ItemArt(item.Kind);
        var r = Perspective.SpriteRect(art[0].Length, art.Length, item.X, item.Z);
        float w = r.W * ItemPixel, h = r.H * ItemPixel;
        return new RectF(r.X + (r.W - w) / 2, r.Bottom - h, w, h);
    }

    private static void DrawItem(Canvas c, ItemInstance item, int tick)
    {
        var sprite = ItemSprite(item.Kind, item.Color);
        var r = ItemRect(item);
        c.Shadow(r.X + r.W / 2, r.Bottom, r.W * 1.2f, r.H * 0.3f + 1, 0.5f);
        if (item.Kind == ItemKind.Eagle)
            c.Glow(r.X + r.W / 2, r.Y + r.H / 2, r.W * 1.4f, new Color(255, 210, 60) * (0.35f + 0.1f * MathF.Sin(tick * 0.1f)));
        c.DrawScaled(sprite, r);
        // A twinkle so objects catch the eye.
        int phase = (tick + item.Id * 17) % 90;
        if (phase < 10)
        {
            float s = (5 - Math.Abs(phase - 5)) * 0.6f;
            var star = new Vector2(r.Right - r.W * 0.2f, r.Y + r.H * 0.2f);
            c.Glow(star.X, star.Y, s + 1.5f, Color.White * 0.8f);
            c.Line(star - new Vector2(s, 0), star + new Vector2(s, 0), Color.White, 0.35f);
            c.Line(star - new Vector2(0, s), star + new Vector2(0, s), Color.White, 0.35f);
        }
    }

    private static void DrawMonster(Canvas c, Monster m, int tick, bool lit)
    {
        string[] art = m.Kind switch
        {
            MonsterKind.Ghost => tick / 10 % 2 == 0 ? SpriteData.Ghost0 : SpriteData.Ghost1,
            MonsterKind.Bat => tick / 5 % 2 == 0 ? SpriteData.Bat0 : SpriteData.Bat1,
            _ => tick / 12 % 2 == 0 ? SpriteData.Knight0 : SpriteData.Knight1,
        };
        var sprite = Sprite.Of(art, m.Kind.ToString());
        var r = Perspective.SpriteRect(sprite.Width, sprite.Height, m.X, m.Z, m.Height);
        if (!lit)
        {
            // In the dark, only a ghostly glow.
            if (m.Kind == MonsterKind.Ghost)
                c.DrawScaledTinted(sprite, r, new Color(60, 80, 220) * 0.6f);
            return;
        }
        var foot = Perspective.Project(m.X, m.Z);
        float k = Perspective.Scale(m.Z);
        c.Shadow(foot.X, foot.Y, sprite.Width * Perspective.SpritePixel * k * (m.Kind == MonsterKind.Knight ? 1f : 0.7f),
            4f * k, m.Kind == MonsterKind.Knight ? 0.55f : 0.3f);
        bool mirror = m.Kind == MonsterKind.Knight && m.Vx < 0 || m.Kind == MonsterKind.Bat && m.Vx < 0;
        if (m.Kind == MonsterKind.Ghost)
        {
            c.Glow(r.X + r.W / 2, r.Y + r.H / 2, r.W, new Color(160, 200, 255) * 0.25f);
            var tint = m.Fleeing && tick / 4 % 2 == 0 ? new Color(120, 255, 255) * 0.75f : Color.White * 0.78f;
            c.DrawScaled(sprite, r, false, tint);
        }
        else
        {
            c.DrawScaled(sprite, r, mirror);
        }
    }

    public static Sprite HeroSprite(Adventure a, out bool mirror)
    {
        int frame = a.Moving ? a.Steps / 7 % 2 : 0;
        mirror = a.Facing == Side.Left;
        string[] art = a.Facing switch
        {
            Side.Back => frame == 0 ? SpriteData.HeroBack0 : SpriteData.HeroBack1,
            Side.Front => frame == 0 ? SpriteData.HeroFront0 : SpriteData.HeroFront1,
            _ => frame == 0 ? SpriteData.HeroSide0 : SpriteData.HeroSide1,
        };
        return Sprite.Of(art, "hero");
    }

    private static void DrawHero(Canvas c, Adventure a, int tick)
    {
        var sprite = HeroSprite(a, out bool mirror);
        var foot = Perspective.Project(a.X, a.Z);
        float k = Perspective.Scale(a.Z);
        var r = Perspective.SpriteRect(sprite.Width, sprite.Height, a.X, a.Z);
        if (a.Has(ItemKind.Lamp))
            c.Glow(r.X + r.W / 2, r.Y + r.H * 0.45f, r.H * 0.9f, new Color(255, 220, 120) * 0.22f);
        c.Shadow(foot.X, foot.Y, sprite.Width * Perspective.SpritePixel * k, 5f * k, 0.6f);
        if (a.HurtTicks < 30 && a.HurtTicks / 3 % 2 == 0)
            c.DrawScaledTinted(sprite, r, new Color(255, 80, 80) * 0.9f, mirror);
        else
            c.DrawScaled(sprite, r, mirror);
    }

    // ---------------------------------------------------------------- panel

    private static readonly Color PanelTop = new(28, 22, 40);
    private static readonly Color Gold = new(230, 186, 70);

    public static void DrawHud(Canvas c, Adventure a, int tick, bool mobile)
    {
        c.ShadePolygon([new(0, Hud.Top), new(Canvas.Width, Hud.Top), new(Canvas.Width, Canvas.Height), new(0, Canvas.Height)],
            PanelTop, (x, y) => 1f - 0.6f * (y - Hud.Top) / (Canvas.Height - Hud.Top));
        c.Fill(0, Hud.Top, Canvas.Width, 0.8f, Gold);
        c.Fill(0, Hud.Top + 1.2f, Canvas.Width, 0.3f, Gold * 0.5f);

        string message = a.MessageTicks > 0 && a.Message != null ? a.Message() : a.Room.Name.ToString();
        var ink = a.MessageTicks > 0 ? Color.White : OricColor.Cyan.ToColor();
        if (message.Length > Canvas.Columns)
            message = message.Substring(0, Canvas.Columns);
        c.TextCentered(Hud.MessageY, message, ink);

        for (int i = 0; i < Adventure.Capacity; i++)
        {
            var r = Hud.Slot(i);
            bool selected = i == a.Selected && i < a.Inventory.Count;
            c.Fill(r.X, r.Y, r.Width, r.Height, selected ? new Color(70, 56, 20) : new Color(16, 14, 26));
            c.Frame(r.X, r.Y, r.Width, r.Height, selected ? Gold : new Color(70, 80, 150), selected ? 0.9f : 0.5f);
            if (i < a.Inventory.Count)
            {
                var item = a.Inventory[i];
                var s = ItemSprite(item.Kind, item.Color);
                float scale = Math.Min((r.Width - 5f) / s.Width, (r.Height - 5f) / s.Height);
                float w = s.Width * scale, h = s.Height * scale;
                c.DrawScaled(s, new RectF(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h));
            }
            else if (!mobile)
            {
                c.Text(r.X + 2, r.Y + 2, (i + 1).ToString(), new Color(70, 80, 150));
            }
        }

        DrawButton(c, Hud.UseButton, Strings.Use + (mobile ? "" : " U"), a.SelectedItem != null);
        DrawButton(c, Hud.DropButton, Strings.Drop + (mobile ? "" : " D"), a.SelectedItem != null);

        c.Text(190, Hud.SlotY + 1, Strings.Score, OricColor.Cyan);
        c.Text(190, Hud.SlotY + 13, a.Score.ToString("D6"), Color.White);

        string energy = Strings.Energy;
        c.Text(3, Hud.StatusY, energy, OricColor.Cyan);
        float bx = 3 + (energy.Length + 1) * 6;
        float bw = 236 - bx;
        c.Fill(bx, Hud.StatusY, bw, 8, new Color(12, 10, 18));
        c.Frame(bx, Hud.StatusY, bw, 8, new Color(200, 200, 210), 0.5f);
        float fill = (bw - 2) * a.Energy / Adventure.MaxEnergy;
        var bar = a.Energy > 50 ? new Color(60, 200, 80) : a.Energy > 20 ? new Color(230, 190, 40) : new Color(230, 50, 40);
        if (a.Energy > 20 || tick / 10 % 2 == 0)
            c.ShadePolygon([new(bx + 1, Hud.StatusY + 1), new(bx + 1 + fill, Hud.StatusY + 1), new(bx + 1 + fill, Hud.StatusY + 7), new(bx + 1, Hud.StatusY + 7)],
                bar, (x, y) => 1.1f - 0.5f * (y - Hud.StatusY) / 8f);
    }

    private static void DrawButton(Canvas c, Rectangle r, string text, bool enabled)
    {
        var colour = enabled ? Gold : new Color(50, 56, 110);
        c.ShadePolygon([new(r.X, r.Y), new(r.Right, r.Y), new(r.Right, r.Bottom), new(r.X, r.Bottom)], colour,
            (x, y) => 1f - 0.35f * (y - r.Y) / r.Height);
        c.Frame(r.X, r.Y, r.Width, r.Height, Color.White * 0.4f, 0.4f);
        c.Text(r.X + (r.Width - text.Length * 6) / 2f, r.Y + 2, text, enabled ? Color.Black : new Color(150, 160, 210));
    }
}
