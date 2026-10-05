using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;
using GoldenEagle.Core.World;

namespace GoldenEagle.Core.Engine;

/// <summary>Draws a room in perspective with its doors, scenery, objects, monsters and the hero, and the panel below.</summary>
public static class RoomRenderer
{
    private const float DoorHeight = 0.62f;

    public static void Draw(Canvas c, Adventure a, int tick, bool mobile)
    {
        DrawRoom(c, a, tick);
        DrawHud(c, a, tick, mobile);
    }

    public static void DrawRoom(Canvas c, Adventure a, int tick)
    {
        var room = a.Room;
        bool lit = a.CanSee(room.Id);
        c.Fill(0, 0, Canvas.Width, Perspective.ViewHeight, OricColor.Black);

        var line = lit ? room.LineColor : OricColor.Blue;
        if (lit)
            DrawSurfaces(c, room);
        DrawEdges(c, line);

        if (lit)
            foreach (var d in room.Decor.Where(d => d.Side != Side.Front))
                DrawDecor(c, d, tick);

        foreach (var door in a.Castle.DoorsOf(room.Id))
            DrawDoor(c, a, door, door.EndIn(room.Id), lit, line);

        if (lit)
            foreach (var pit in room.Pits)
                DrawPit(c, pit, room.LineColor);

        // Everything standing in the room, far to near.
        var things = new List<(float z, Action draw)>();
        if (lit)
            foreach (var item in a.ItemsIn(room.Id))
                things.Add((item.Z, () => DrawItem(c, item, tick)));
        foreach (var m in a.MonstersIn(room.Id))
            things.Add((m.Z, () => DrawMonster(c, m, tick, lit)));
        things.Add((a.Z, () => DrawHero(c, a)));
        foreach (var t in things.OrderByDescending(t => t.z))
            t.draw();
    }

    private static Point P(float x, float z, float h = 0) => Perspective.Project(x, z, h);

    private static void DrawSurfaces(Canvas c, RoomDef room)
    {
        // Back wall, side walls (shaded with a dither, as the Oric did) and the floor.
        c.FillPolygon([P(-1, 1, 1), P(1, 1, 1), P(1, 1, 0), P(-1, 1, 0)], room.WallColor);
        c.DitherPolygon([P(-1, 0, 1), P(-1, 1, 1), P(-1, 1, 0), P(-1, 0, 0)], room.WallColor, OricColor.Black);
        c.DitherPolygon([P(1, 1, 1), P(1, 0, 1), P(1, 0, 0), P(1, 1, 0)], room.WallColor, OricColor.Black);
        c.FillPolygon([P(-1, 1, 0), P(1, 1, 0), P(1, 0, 0), P(-1, 0, 0)], room.FloorColor);

        // Flagstones.
        if (room.FloorLine != room.FloorColor)
        {
            for (float z = 0.125f; z < 1f; z += 0.25f)
                c.Line(P(-1, z), P(1, z), room.FloorLine);
            for (float x = -0.75f; x < 1f; x += 0.25f)
                c.Line(P(x, 0), P(x, 1), room.FloorLine);
        }
    }

    private static void DrawEdges(Canvas c, OricColor line)
    {
        var bl = P(-1, 1, 0);
        var br = P(1, 1, 0);
        var tl = P(-1, 1, 1);
        var tr = P(1, 1, 1);
        c.Line(tl, tr, line);
        c.Line(bl, br, line);
        c.Line(tl, bl, line);
        c.Line(tr, br, line);
        c.Line(P(-1, 0, 0), bl, line);
        c.Line(P(1, 0, 0), br, line);
        c.Line(P(-1, 0, 1), tl, line);
        c.Line(P(1, 0, 1), tr, line);
    }

    // ---------------------------------------------------------------- doors

    /// <summary>Screen outline of a door (four corners), or null for front exits.</summary>
    public static Point[] DoorQuad(DoorEnd end)
    {
        float p = end.Position;
        return end.Side switch
        {
            Side.Back =>
            [
                P(p - Adventure.DoorHalfWidth, 1, DoorHeight), P(p + Adventure.DoorHalfWidth, 1, DoorHeight),
                P(p + Adventure.DoorHalfWidth, 1, 0), P(p - Adventure.DoorHalfWidth, 1, 0),
            ],
            Side.Left =>
            [
                P(-1, p - Adventure.SideDoorHalfDepth, DoorHeight), P(-1, p + Adventure.SideDoorHalfDepth, DoorHeight),
                P(-1, p + Adventure.SideDoorHalfDepth, 0), P(-1, p - Adventure.SideDoorHalfDepth, 0),
            ],
            Side.Right =>
            [
                P(1, p + Adventure.SideDoorHalfDepth, DoorHeight), P(1, p - Adventure.SideDoorHalfDepth, DoorHeight),
                P(1, p - Adventure.SideDoorHalfDepth, 0), P(1, p + Adventure.SideDoorHalfDepth, 0),
            ],
            _ =>
            [
                P(p - Adventure.DoorHalfWidth, 0.07f), P(p + Adventure.DoorHalfWidth, 0.07f),
                P(p + Adventure.DoorHalfWidth + 0.03f, 0), P(p - Adventure.DoorHalfWidth - 0.03f, 0),
            ],
        };
    }

    /// <summary>Bounding box of a door on screen, enlarged a little for tapping.</summary>
    public static Rectangle DoorBounds(DoorEnd end)
    {
        var q = DoorQuad(end);
        int x0 = q.Min(p => p.X), x1 = q.Max(p => p.X), y0 = q.Min(p => p.Y), y1 = q.Max(p => p.Y);
        var r = new Rectangle(x0 - 3, y0 - 3, x1 - x0 + 6, y1 - y0 + 6);
        if (end.Side == Side.Front)
            r = new Rectangle(r.X, r.Y - 4, r.Width, Perspective.ViewHeight - (r.Y - 4));
        return r;
    }

    private static void DrawDoor(Canvas c, Adventure a, Door door, DoorEnd end, bool lit, OricColor line)
    {
        var q = DoorQuad(end);
        bool locked = !a.IsUnlocked(door);
        if (end.Side == Side.Front)
        {
            // The front wall is behind the viewer: its exits are marked on the floor.
            var mark = end.Kind == ExitKind.Gate ? OricColor.Yellow : locked ? door.LockColor!.Value : OricColor.Black;
            if (lit || end.Kind == ExitKind.Gate)
                c.FillPolygon(q, mark == OricColor.Black ? OricColor.Black : mark);
            c.Line(q[0], q[1], line);
            c.Line(q[1], q[2], line);
            c.Line(q[3], q[0], line);
            int cx = (q[0].X + q[1].X) / 2;
            int y = q[0].Y + 1;
            var arrow = end.Kind == ExitKind.Gate ? OricColor.Red : end.Kind == ExitKind.Door ? line : OricColor.White;
            c.Line(cx - 3, y, cx, y + 3, arrow);
            c.Line(cx + 3, y, cx, y + 3, arrow);
            if (end.Kind != ExitKind.Door)
                DrawStairMark(c, end.Kind, cx, y - 9, arrow);
            return;
        }

        c.FillPolygon(q, locked && lit ? door.LockColor!.Value : OricColor.Black);
        if (end.Kind is ExitKind.StairsUp or ExitKind.StairsDown && lit)
            DrawSteps(c, q, end.Kind == ExitKind.StairsUp);
        c.Line(q[0], q[1], line);
        c.Line(q[1], q[2], line);
        c.Line(q[2], q[3], line);
        c.Line(q[3], q[0], line);
        if (locked && lit)
        {
            // Keyhole.
            int kx = (q[0].X + q[1].X + q[2].X + q[3].X) / 4;
            int ky = (q[0].Y + q[1].Y + q[2].Y + q[3].Y) / 4;
            c.Fill(kx - 1, ky - 2, 3, 3, OricColor.Black);
            c.Fill(kx, ky, 1, 3, OricColor.Black);
        }
        if (end.Kind is ExitKind.StairsUp or ExitKind.StairsDown)
            DrawStairMark(c, end.Kind, (q[0].X + q[1].X) / 2, Math.Min(q[0].Y, q[1].Y) - 8, OricColor.White);
    }

    private static void DrawSteps(Canvas c, Point[] q, bool up)
    {
        int top = Math.Max(q[0].Y, q[1].Y), bottom = Math.Min(q[2].Y, q[3].Y);
        int left = Math.Max(q[0].X, q[3].X), right = Math.Min(q[1].X, q[2].X);
        if (right - left < 3 || bottom - top < 4)
            return;
        int steps = 5;
        for (int i = 0; i < steps; i++)
        {
            int y = bottom - (bottom - top) * (i + 1) / (steps + 1);
            int inset = (right - left) * i / (steps * 3);
            c.Line(left + inset, y, right - inset, y, up ? OricColor.White : OricColor.Cyan);
        }
    }

    private static void DrawStairMark(Canvas c, ExitKind kind, int x, int y, OricColor color)
    {
        // A small arrow: up or down.
        if (kind == ExitKind.StairsUp)
        {
            c.Line(x, y, x, y + 6, color);
            c.Line(x - 3, y + 3, x, y, color);
            c.Line(x + 3, y + 3, x, y, color);
        }
        else if (kind == ExitKind.StairsDown)
        {
            c.Line(x, y, x, y + 6, color);
            c.Line(x - 3, y + 3, x, y + 6, color);
            c.Line(x + 3, y + 3, x, y + 6, color);
        }
    }

    private static void DrawPit(Canvas c, Pit pit, OricColor line)
    {
        Point[] q = [P(pit.X0, pit.Z1), P(pit.X1, pit.Z1), P(pit.X1, pit.Z0), P(pit.X0, pit.Z0)];
        c.FillPolygon(q, OricColor.Black);
        c.Line(q[0], q[1], line);
        c.Line(q[1], q[2], line);
        c.Line(q[2], q[3], line);
        c.Line(q[3], q[0], line);
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

    private static void DrawDecor(Canvas c, Decor d, int tick)
    {
        var (art, width, h0, h1) = DecorArt(d.Kind, tick);
        var sprite = Sprite.Of(art, d.Kind.ToString());
        Rectangle r;
        bool mirror = false;
        if (d.Side == Side.Back)
        {
            var a = P(d.Position - width / 2, 1, h1);
            var b = P(d.Position + width / 2, 1, h0);
            r = new Rectangle(a.X, a.Y, b.X - a.X, b.Y - a.Y);
        }
        else
        {
            // On a side wall the picture is squeezed into its perspective box.
            float x = d.Side == Side.Left ? -1 : 1;
            float half = width * 0.28f;
            var near0 = P(x, d.Position - half, h1);
            var far1 = P(x, d.Position + half, h0);
            var near1 = P(x, d.Position - half, h0);
            int left = Math.Min(near0.X, far1.X), right = Math.Max(near0.X, far1.X);
            r = new Rectangle(left, near0.Y, Math.Max(1, right - left), Math.Max(1, near1.Y - near0.Y));
            mirror = d.Side == Side.Right;
            if (d.Kind is DecorKind.Cobweb && d.Side == Side.Left)
                mirror = false;
        }
        c.DrawScaled(sprite, r, mirror);
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
        var art = ItemArt(kind);
        var sprite = Sprite.Of(art, kind.ToString());
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

    public static Rectangle ItemRect(ItemInstance item)
    {
        var art = ItemArt(item.Kind);
        return Perspective.SpriteRect(art[0].Length, art.Length, item.X, item.Z);
    }

    private static void DrawItem(Canvas c, ItemInstance item, int tick)
    {
        var sprite = ItemSprite(item.Kind, item.Color);
        var r = ItemRect(item);
        DrawShadow(c, item.X, item.Z, sprite.Width);
        c.DrawScaled(sprite, r);
        // A glint so objects catch the eye.
        if ((tick + item.Id * 17) % 90 < 6)
            c.Plot(r.Right - 1, r.Top, OricColor.White);
    }

    private static void DrawShadow(Canvas c, float x, float z, int width)
    {
        var foot = Perspective.Project(x, z);
        int w = Math.Max(2, (int)(width * Perspective.SpritePixel * Perspective.Scale(z) * 0.8f));
        c.Fill(foot.X - w / 2, foot.Y - 1, w, 2, OricColor.Black);
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
        float h = m.Height;
        var r = Perspective.SpriteRect(sprite.Width, sprite.Height, m.X, m.Z, h);
        if (!lit)
        {
            // In the dark, only a ghostly glow.
            if (m.Kind == MonsterKind.Ghost)
                c.DrawScaledTinted(sprite, r, OricColor.Blue);
            return;
        }
        if (m.Kind != MonsterKind.Knight)
            DrawShadow(c, m.X, m.Z, sprite.Width / 2);
        bool mirror = m.Kind == MonsterKind.Knight && m.Vx < 0;
        if (m.Kind == MonsterKind.Ghost && m.Fleeing && tick / 4 % 2 == 0)
            c.DrawScaledTinted(sprite, r, OricColor.Cyan);
        else
            c.DrawScaled(sprite, r, mirror);
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

    private static void DrawHero(Canvas c, Adventure a)
    {
        if (a.HurtTicks < 30 && a.HurtTicks / 3 % 2 == 0)
            return;
        var sprite = HeroSprite(a, out bool mirror);
        DrawShadow(c, a.X, a.Z, sprite.Width);
        c.DrawScaled(sprite, Perspective.SpriteRect(sprite.Width, sprite.Height, a.X, a.Z), mirror);
    }

    // ---------------------------------------------------------------- panel

    public static void DrawHud(Canvas c, Adventure a, int tick, bool mobile)
    {
        c.Fill(0, Hud.Top, Canvas.Width, Canvas.Height - Hud.Top, OricColor.Black);
        c.Fill(0, Hud.Top, Canvas.Width, 1, OricColor.Yellow);

        string message = a.MessageTicks > 0 && a.Message != null ? a.Message() : a.Room.Name.ToString();
        var ink = a.MessageTicks > 0 ? OricColor.White : OricColor.Cyan;
        if (message.Length > Canvas.Columns)
            message = message.Substring(0, Canvas.Columns);
        c.TextCentered(Hud.MessageY, message, ink);

        for (int i = 0; i < Adventure.Capacity; i++)
        {
            var r = Hud.Slot(i);
            bool selected = i == a.Selected;
            c.Frame(r.X, r.Y, r.Width, r.Height, selected ? OricColor.Yellow : OricColor.Blue);
            if (selected)
                c.Frame(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2, OricColor.Yellow);
            if (i < a.Inventory.Count)
            {
                var item = a.Inventory[i];
                var s = ItemSprite(item.Kind, item.Color);
                int scale = Math.Max(1, Math.Min((r.Width - 6) / s.Width, (r.Height - 6) / s.Height));
                int w = s.Width * scale, h = s.Height * scale;
                c.DrawScaled(s, new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h));
            }
            else if (!mobile)
            {
                c.Text(r.X + 2, r.Y + 2, (i + 1).ToString(), OricColor.Blue);
            }
        }

        DrawButton(c, Hud.UseButton, Strings.Use + (mobile ? "" : " U"), a.SelectedItem != null);
        DrawButton(c, Hud.DropButton, Strings.Drop + (mobile ? "" : " D"), a.SelectedItem != null);

        c.Text(190, Hud.SlotY + 1, Strings.Score, OricColor.Cyan);
        c.Text(190, Hud.SlotY + 13, a.Score.ToString("D6"), OricColor.White);

        string energy = Strings.Energy;
        c.Text(3, Hud.StatusY, energy, OricColor.Cyan);
        int bx = 3 + (energy.Length + 1) * 6;
        int bw = 236 - bx;
        c.Frame(bx, Hud.StatusY, bw, 8, OricColor.White);
        int fill = (bw - 4) * a.Energy / Adventure.MaxEnergy;
        var barColor = a.Energy > 50 ? OricColor.Green : a.Energy > 20 ? OricColor.Yellow : OricColor.Red;
        if (a.Energy > 20 || tick / 10 % 2 == 0)
            c.Fill(bx + 2, Hud.StatusY + 2, fill, 4, barColor);
    }

    private static void DrawButton(Canvas c, Rectangle r, string text, bool enabled)
    {
        var colour = enabled ? OricColor.Yellow : OricColor.Blue;
        c.Fill(r.X, r.Y, r.Width, r.Height, colour);
        c.Text(r.X + (r.Width - text.Length * 6) / 2, r.Y + 2, text, OricColor.Black);
    }
}
