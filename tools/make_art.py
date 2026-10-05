#!/usr/bin/env python3
"""Draws the game's high-resolution artwork into GoldenEagle.Core/Art/*.png.

    python3 tools/make_art.py

Every picture is drawn as vector shapes (polygons, ellipses, gradients) at 4x supersampling and
reduced with a Lanczos filter, so edges are smooth at any screen size. Each picture has the same
proportions as its pixel map in SpriteData.cs (one map pixel = one "unit" here), which the game
still uses for layout; the PNG is drawn into that box at PX pixels per unit.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'GoldenEagle.Core', 'Art')
PX = 16          # output pixels per unit
SS = 4           # supersampling

INK = (34, 22, 20, 255)
SKIN = (242, 204, 168)
SKIN_D = (206, 160, 124)
GOLD_L = (255, 228, 120)
GOLD = (232, 182, 56)
GOLD_D = (168, 112, 24)
GOLD_INK = (96, 58, 14, 255)
STEEL_L = (226, 232, 242)
STEEL = (168, 178, 196)
STEEL_D = (96, 104, 124)
WOOD_L = (176, 116, 62)
WOOD = (130, 80, 40)
WOOD_D = (84, 50, 24)
IRON = (70, 70, 80)

COLOURS = {
    'Red': ((236, 80, 64), (150, 26, 22)),
    'Green': ((90, 214, 104), (24, 120, 48)),
    'Blue': ((96, 140, 245), (30, 50, 160)),
    'Yellow': ((255, 222, 90), (190, 140, 20)),
    'Magenta': ((236, 96, 226), (130, 30, 130)),
    'Cyan': ((100, 236, 240), (20, 130, 150)),
}


def rgba(c, a=255):
    return tuple(c[:3]) + (a,)


class Pic:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.k = PX * SS
        self.im = Image.new('RGBA', (round(w * self.k), round(h * self.k)), (0, 0, 0, 0))

    def P(self, pts):
        return [(x * self.k, y * self.k) for x, y in pts]

    def B(self, box):
        x0, y0, x1, y1 = box
        return [x0 * self.k, y0 * self.k, x1 * self.k, y1 * self.k]

    def _layer(self):
        return Image.new('RGBA', self.im.size, (0, 0, 0, 0))

    def _mask(self, kind, geo, **kw):
        m = Image.new('L', self.im.size, 0)
        d = ImageDraw.Draw(m)
        if kind == 'poly':
            d.polygon(self.P(geo), fill=255)
        elif kind == 'ellipse':
            d.ellipse(self.B(geo), fill=255)
        elif kind == 'rrect':
            d.rounded_rectangle(self.B(geo), radius=kw.get('r', 0.5) * self.k, fill=255)
        return m

    def _gradient(self, c0, c1, angle=90, box=None):
        """Linear gradient across the whole image (angle 90 = top to bottom), optionally over a box."""
        W, H = self.im.size
        x0, y0, x1, y1 = (0, 0, self.w, self.h) if box is None else box
        g = Image.new('RGBA', (W, H))
        a = math.radians(angle)
        dx, dy = math.cos(a), math.sin(a)
        # Project the box corners to find the gradient range.
        corners = [(x0, y0), (x1, y0), (x0, y1), (x1, y1)]
        proj = [cx * dx + cy * dy for cx, cy in corners]
        lo, hi = min(proj), max(proj)
        lo *= self.k
        hi *= self.k
        steps = 256
        grad = Image.new('RGBA', (steps, 1))
        for i in range(steps):
            t = i / (steps - 1)
            grad.putpixel((i, 0), tuple(int(c0[j] + (c1[j] - c0[j]) * t) for j in range(3)) + (
                int((c0[3] if len(c0) > 3 else 255) + ((c1[3] if len(c1) > 3 else 255) - (c0[3] if len(c0) > 3 else 255)) * t),))
        # Build per-pixel t with numpy for speed.
        import numpy as np
        yy, xx = np.mgrid[0:H, 0:W]
        p = xx * dx + yy * dy
        t = np.clip((p - lo) / max(1e-6, hi - lo), 0, 1)
        idx = (t * (steps - 1)).astype(np.int32)
        arr = np.array(grad)[0][idx]
        return Image.fromarray(arr.astype('uint8'))

    def fill(self, kind, geo, colour, outline=INK, width=0.22, grad=None, angle=90, **kw):
        """Fills a shape with a colour or a gradient (grad=(c0, c1)) and strokes its outline."""
        m = self._mask(kind, geo, **kw)
        if grad:
            xs = [p[0] for p in geo] if kind == 'poly' else [geo[0], geo[2]]
            ys = [p[1] for p in geo] if kind == 'poly' else [geo[1], geo[3]]
            src = self._gradient(rgba(grad[0], grad[0][3] if len(grad[0]) > 3 else 255),
                                 rgba(grad[1], grad[1][3] if len(grad[1]) > 3 else 255),
                                 angle, (min(xs), min(ys), max(xs), max(ys)))
        else:
            src = Image.new('RGBA', self.im.size, rgba(colour, colour[3] if len(colour) > 3 else 255))
        layer = self._layer()
        layer.paste(src, (0, 0), m)
        self.im = Image.alpha_composite(self.im, layer)
        if outline and width:
            self.stroke(kind, geo, outline, width, **kw)

    def stroke(self, kind, geo, colour, width, **kw):
        d = ImageDraw.Draw(self.im)
        w = max(1, round(width * self.k))
        if kind == 'poly':
            pts = self.P(geo)
            d.line(pts + [pts[0]], fill=rgba(colour), width=w, joint='curve')
            r = w / 2
            for x, y in pts:
                d.ellipse([x - r, y - r, x + r, y + r], fill=rgba(colour))
        elif kind == 'ellipse':
            d.ellipse(self.B(geo), outline=rgba(colour), width=w)
        elif kind == 'rrect':
            d.rounded_rectangle(self.B(geo), radius=kw.get('r', 0.5) * self.k, outline=rgba(colour), width=w)

    def poly(self, pts, colour, **kw):
        self.fill('poly', pts, colour, **kw)

    def ellipse(self, box, colour, **kw):
        self.fill('ellipse', box, colour, **kw)

    def rrect(self, box, colour, r=0.5, **kw):
        self.fill('rrect', box, colour, r=r, **kw)

    def circle(self, cx, cy, r, colour, **kw):
        self.fill('ellipse', (cx - r, cy - r, cx + r, cy + r), colour, **kw)

    def line(self, pts, colour, width=0.2):
        d = ImageDraw.Draw(self.im)
        w = max(1, round(width * self.k))
        d.line(self.P(pts), fill=rgba(colour, colour[3] if len(colour) > 3 else 255), width=w, joint='curve')

    def glow(self, cx, cy, r, colour, strength=0.6):
        # Keep the blurred light inside the picture so it never shows a hard edge.
        r = min(r, cx / 1.4, (self.w - cx) / 1.4, cy / 1.4, (self.h - cy) / 1.4)
        if r <= 0.2:
            return
        layer = self._layer()
        d = ImageDraw.Draw(layer)
        d.ellipse(self.B((cx - r, cy - r, cx + r, cy + r)), fill=rgba(colour, int(255 * strength)))
        layer = layer.filter(ImageFilter.GaussianBlur(r * self.k * 0.45))
        self.im = Image.alpha_composite(self.im, layer)

    def save(self, name):
        os.makedirs(OUT, exist_ok=True)
        out = self.im.resize((round(self.w * PX), round(self.h * PX)), Image.LANCZOS)
        out.save(os.path.join(OUT, name + '.png'), optimize=True)


def arc(cx, cy, rx, ry, a0, a1, n=16):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def mirror(pts, w):
    return [(w - x, y) for x, y in pts]


# ------------------------------------------------------------------ the hero

TUNIC = ((70, 176, 96), (26, 104, 52))
HAT = ((226, 64, 56), (142, 22, 26))
HOSE = ((76, 100, 200), (36, 50, 130))
BOOT = ((150, 92, 46), (84, 48, 22))


def hero_front(walk, back=False):
    p = Pic(12, 24)
    # Legs and boots (behind the tunic).
    left_lift = 1.0 if walk else 0.0
    p.rrect((3.7, 15.0, 5.6, 21.6 - left_lift), None, r=0.4, grad=HOSE, angle=0)
    p.rrect((6.4, 15.0, 8.3, 21.6), None, r=0.4, grad=HOSE, angle=0)
    p.rrect((3.0, 20.6 - left_lift, 5.9, 23.9 - left_lift), None, r=0.6, grad=BOOT)
    p.rrect((6.1, 20.6, 9.0, 23.9), None, r=0.6, grad=BOOT)
    # Arms (swinging when walking).
    la, ra = (-0.8, 0.8) if walk else (0, 0)
    p.poly([(3.2, 8.9), (2.1, 9.4), (1.6 + la * 0.3, 14.0 + la), (2.9 + la * 0.3, 14.1 + la), (3.6, 10.6)], None, grad=TUNIC)
    p.poly([(8.8, 8.9), (9.9, 9.4), (10.4 - ra * 0.3, 14.0 + ra), (9.1 - ra * 0.3, 14.1 + ra), (8.4, 10.6)], None, grad=TUNIC)
    p.circle(2.25 + la * 0.3, 14.5 + la, 0.75, SKIN)
    p.circle(9.75 - ra * 0.3, 14.5 + ra, 0.75, SKIN)
    # Tunic.
    p.poly([(3.2, 8.4), (8.8, 8.4), (9.5, 15.9), (7.6, 16.4), (6.0, 15.8), (4.4, 16.4), (2.5, 15.9)], None, grad=TUNIC)
    if not back:
        p.poly([(5.0, 8.5), (7.0, 8.5), (6.0, 10.4)], SKIN_D, width=0.12)
    p.rrect((2.8, 12.3, 9.2, 13.4), WOOD_D, r=0.2, width=0.15)
    p.rrect((5.3, 12.1, 6.7, 13.6), GOLD, r=0.2, width=0.15)
    # Neck and head.
    p.rrect((5.2, 7.4, 6.8, 9.0), SKIN, r=0.3, width=0.15)
    if back:
        p.ellipse((3.4, 2.4, 8.6, 8.2), None, grad=((214, 162, 74), (150, 100, 40)))
    else:
        p.ellipse((3.4, 2.4, 8.6, 8.2), None, grad=(SKIN, SKIN_D))
        p.ellipse((4.4, 4.7, 5.3, 5.8), (40, 40, 60), outline=None)
        p.ellipse((6.7, 4.7, 7.6, 5.8), (40, 40, 60), outline=None)
        p.circle(4.95, 4.95, 0.18, (255, 255, 255), outline=None)
        p.circle(7.25, 4.95, 0.18, (255, 255, 255), outline=None)
        p.line(arc(6.0, 6.2, 0.9, 0.6, 20, 160, 8), (150, 60, 50), 0.18)
        p.ellipse((3.6, 5.8, 4.6, 6.6), (240, 150, 140, 120), outline=None)
        p.ellipse((7.4, 5.8, 8.4, 6.6), (240, 150, 140, 120), outline=None)
    # Hat with a feather.
    p.poly([(8.1, 2.4), (10.6, 0.2), (11.0, 0.5), (9.0, 2.8)], (255, 236, 150), width=0.15)
    p.poly([(3.0, 3.8), (3.6, 1.6), (6.0, 0.5), (8.4, 1.6), (9.0, 3.8)], None, grad=HAT)
    p.rrect((2.6, 3.2, 9.4, 4.3), None, r=0.4, grad=HAT)
    return p


def hero_side(walk):
    p = Pic(12, 24)
    if walk:
        # Back leg behind, front leg forward.
        p.poly([(5.0, 15.2), (6.6, 15.2), (5.0, 21.6), (3.2, 21.3)], None, grad=HOSE, angle=0)
        p.rrect((1.6, 20.6, 4.9, 23.6), None, r=0.6, grad=BOOT)
        p.poly([(5.6, 15.2), (7.2, 15.2), (9.0, 21.3), (7.3, 21.7)], None, grad=HOSE, angle=0)
        p.rrect((7.0, 20.8, 10.6, 23.9), None, r=0.6, grad=BOOT)
    else:
        p.rrect((5.0, 15.0, 6.8, 21.6), None, r=0.4, grad=HOSE, angle=0)
        p.rrect((4.6, 20.6, 8.6, 23.9), None, r=0.6, grad=BOOT)
    p.poly([(4.3, 8.4), (8.1, 8.4), (8.7, 15.9), (3.7, 15.9)], None, grad=TUNIC)
    p.rrect((3.9, 12.3, 8.5, 13.4), WOOD_D, r=0.2, width=0.15)
    swing = 1.2 if walk else 0
    p.poly([(5.4, 9.0), (7.0, 9.0), (7.4 + swing, 13.8), (6.0 + swing, 14.0)], None, grad=TUNIC)
    p.circle(6.8 + swing, 14.4, 0.75, SKIN)
    p.rrect((5.4, 7.4, 7.0, 9.0), SKIN, r=0.3, width=0.15)
    p.ellipse((3.9, 2.4, 8.6, 8.2), None, grad=(SKIN, SKIN_D))
    p.poly([(8.3, 4.9), (9.4, 6.0), (8.3, 6.4)], SKIN, width=0.15)
    p.ellipse((7.0, 4.6, 7.9, 5.7), (40, 40, 60), outline=None)
    p.circle(7.55, 4.85, 0.17, (255, 255, 255), outline=None)
    p.ellipse((3.9, 3.0, 6.2, 7.0), (214, 162, 74), width=0.12)
    p.poly([(4.2, 2.6), (1.0, 0.4), (0.8, 0.8), (3.8, 3.2)], (255, 236, 150), width=0.15)
    p.poly([(3.8, 3.8), (4.3, 1.6), (6.4, 0.5), (8.4, 1.6), (8.8, 3.8)], None, grad=HAT)
    p.rrect((3.4, 3.2, 10.2, 4.3), None, r=0.4, grad=HAT)
    return p


# ------------------------------------------------------------------ monsters

def ghost(phase):
    p = Pic(12, 14)
    top = arc(6, 6, 5.6, 5.6, 180, 360, 24)
    hem = []
    for i in range(13):
        x = 11.6 - i * (11.2 / 12)
        y = 12.4 + 1.1 * math.sin(i * math.pi / 2 + phase * math.pi)
        hem.append((x, y))
    shape = top + [(11.6, 12.0)] + hem + [(0.4, 12.0)]
    p.glow(6, 7, 6.5, (180, 210, 255), 0.35)
    p.poly(shape, None, grad=((255, 255, 255, 235), (190, 205, 240, 200)), outline=(120, 140, 190), width=0.18)
    p.ellipse((3.1, 3.8, 5.3, 6.8), (30, 30, 50), outline=None)
    p.ellipse((6.7, 3.8, 8.9, 6.8), (30, 30, 50), outline=None)
    p.circle(4.6, 4.6, 0.35, (255, 255, 255), outline=None)
    p.circle(8.2, 4.6, 0.35, (255, 255, 255), outline=None)
    p.ellipse((5.0, 7.6, 7.0, 10.0), (40, 30, 60), outline=None)
    return p


def bat(up):
    p = Pic(14, 6)
    wing = ((150, 60, 150), (80, 24, 90))
    if up:
        left = [(6.0, 2.6), (3.6, 1.0), (0.3, 0.2), (1.2, 2.4), (2.4, 2.1), (3.0, 3.6), (4.4, 3.0), (5.4, 4.2)]
    else:
        left = [(6.0, 2.4), (3.6, 2.8), (0.4, 5.6), (2.0, 5.0), (2.8, 5.8), (4.0, 4.6), (5.0, 5.2), (5.8, 3.8)]
    p.poly(left, None, grad=wing)
    p.poly(mirror(left, 14), None, grad=wing)
    p.poly([(6.1, 1.7), (6.4, 0.4), (6.9, 1.4), (7.1, 1.4), (7.6, 0.4), (7.9, 1.7)], (90, 30, 90), width=0.15)
    p.ellipse((5.6, 1.2, 8.4, 5.4), None, grad=((120, 50, 120), (60, 16, 66)))
    p.circle(6.4, 2.6, 0.38, (255, 60, 40), outline=None)
    p.circle(7.6, 2.6, 0.38, (255, 60, 40), outline=None)
    p.poly([(6.5, 3.8), (6.8, 4.5), (7.0, 3.8)], (255, 255, 255), width=0.06)
    p.poly([(7.0, 3.8), (7.2, 4.5), (7.5, 3.8)], (255, 255, 255), width=0.06)
    return p


def knight(walk):
    p = Pic(12, 24)
    steel = (STEEL_L, STEEL_D)
    # Spear behind the hand.
    p.rrect((10.4, 0.6, 11.1, 24.0), None, r=0.2, grad=(WOOD_L, WOOD_D), angle=0, width=0.15)
    p.poly([(10.75, -0.2), (11.5, 1.6), (10.75, 2.4), (10.0, 1.6)], None, grad=steel, angle=0, width=0.15)
    if walk:
        p.poly([(4.0, 15.4), (5.8, 15.4), (4.4, 21.8), (2.6, 21.6)], None, grad=steel, angle=0)
        p.poly([(6.2, 15.4), (8.0, 15.4), (8.8, 21.6), (7.0, 21.8)], None, grad=steel, angle=0)
        p.rrect((1.4, 21.0, 4.8, 23.9), None, r=0.5, grad=steel)
        p.rrect((6.8, 21.0, 10.0, 23.9), None, r=0.5, grad=steel)
    else:
        p.rrect((3.8, 15.4, 5.7, 21.8), None, r=0.4, grad=steel, angle=0)
        p.rrect((6.3, 15.4, 8.2, 21.8), None, r=0.4, grad=steel, angle=0)
        p.rrect((3.0, 21.0, 5.9, 23.9), None, r=0.5, grad=steel)
        p.rrect((6.1, 21.0, 9.0, 23.9), None, r=0.5, grad=steel)
    p.circle(4.75, 18.4, 0.9, STEEL_L, width=0.15)
    p.circle(7.25, 18.4, 0.9, STEEL_L, width=0.15)
    # Arms; the right hand grips the spear.
    p.poly([(2.6, 8.6), (1.4, 9.4), (1.6, 14.4), (2.9, 14.4), (3.4, 10.4)], None, grad=steel)
    p.poly([(9.4, 8.6), (10.6, 9.2), (11.4, 12.2), (10.2, 12.8), (8.8, 10.4)], None, grad=steel)
    p.circle(2.25, 14.8, 0.8, STEEL_D)
    p.circle(10.75, 12.4, 0.85, STEEL_D)
    # Breastplate, belt and tassets.
    p.poly([(2.8, 7.8), (9.2, 7.8), (8.8, 13.0), (3.2, 13.0)], None, grad=steel, angle=0)
    p.line([(6.0, 8.2), (6.0, 12.6)], STEEL_L, 0.25)
    p.rrect((3.0, 12.6, 9.0, 13.6), (110, 40, 40), r=0.2, width=0.15)
    p.poly([(3.0, 13.6), (9.0, 13.6), (9.4, 16.0), (2.6, 16.0)], None, grad=steel)
    p.line([(4.5, 13.8), (4.4, 15.8)], STEEL_D, 0.15)
    p.line([(7.5, 13.8), (7.6, 15.8)], STEEL_D, 0.15)
    p.ellipse((1.4, 7.4, 4.4, 10.2), None, grad=steel)
    p.ellipse((7.6, 7.4, 10.6, 10.2), None, grad=steel)
    # Helmet with visor and plume.
    p.poly([(6.0, 1.8), (4.0, 0.0), (6.6, 0.5), (8.4, 0.2), (7.2, 1.8)], None, grad=HAT, width=0.15)
    p.rrect((3.5, 1.6, 8.5, 7.6), None, r=1.6, grad=steel, angle=0)
    p.rrect((4.2, 4.0, 7.8, 4.7), (20, 20, 30), r=0.2, outline=None)
    for x in (5.0, 6.0, 7.0):
        p.circle(x, 5.9, 0.18, (30, 30, 40), outline=None)
    p.line([(6.0, 1.9), (6.0, 3.8)], STEEL_L, 0.25)
    return p


# ------------------------------------------------------------------ the golden eagle

def draw_eagle(p, ox, oy, s, gold=(GOLD_L, GOLD_D), ink=GOLD_INK):
    """A heraldic spread eagle, head turned left, in a 47 x 27 unit box scaled by s at (ox, oy)."""
    T = lambda pts: [(ox + x * s, oy + y * s) for x, y in pts]
    w = 0.32 * s

    def wing(pts_side):
        return pts_side

    # Wings: coverts, then a fan of long feathers hanging from the lower edge.
    left_coverts = [(21.0, 8.6), (16.0, 5.4), (9.0, 2.6), (1.4, 0.2), (2.8, 3.0), (8.0, 6.6), (14.0, 10.0), (20.6, 13.0)]
    tips = [(0.6, 3.0), (1.4, 7.0), (3.0, 10.6), (5.6, 13.6), (9.0, 15.8), (12.8, 17.0), (16.4, 17.4)]
    bases = [(2.8, 3.0), (5.0, 4.6), (8.0, 6.6), (10.8, 8.2), (14.0, 10.0), (17.0, 11.4), (20.0, 12.6)]
    for side in (0, 1):
        f = (lambda pts: pts) if side == 0 else (lambda pts: mirror(pts, 47))
        for i in range(len(tips) - 1, -1, -1):
            b0 = bases[i]
            b1 = bases[i + 1] if i + 1 < len(bases) else (21.0, 13.4)
            tip = tips[i]
            feather = [b0, (tip[0] + 0.2, tip[1] - 0.6), (tip[0] + 0.9, tip[1] + 0.3), b1]
            p.poly(T(f(feather)), None, grad=(gold[0], gold[1]), angle=60 if side == 0 else 120, outline=ink, width=w * 0.8)
            mid = ((b0[0] + b1[0]) / 2, (b0[1] + b1[1]) / 2)
            p.line(T(f([mid, (tip[0] + 0.5, tip[1] - 0.1)])), rgba(ink[:3], 160), 0.12 * s)
        p.poly(T(f(left_coverts)), None, grad=(gold[0], gold[1]), angle=80, outline=ink, width=w)
        for j in range(1, 4):
            t = j / 4
            a = (21.0 + (1.4 - 21.0) * t, 8.6 + (0.2 - 8.6) * t + 0.4)
            b = (20.6 + (2.8 - 20.6) * t, 13.0 + (3.0 - 13.0) * t - 0.4)
            p.line(T(f([a, ((a[0] + b[0]) / 2 - 0.6, (a[1] + b[1]) / 2), b])), rgba(ink[:3], 140), 0.12 * s)
    # Tail fan.
    for i, (tx, ty) in enumerate([(19.4, 25.0), (21.4, 26.4), (23.5, 26.9), (25.6, 26.4), (27.6, 25.0)]):
        p.poly(T([(22.2, 18.0), (tx - 0.9, ty - 0.3), (tx, ty), (tx + 0.9, ty - 0.3), (24.8, 18.0)]), None,
               grad=(gold[0], gold[1]), outline=ink, width=w * 0.8)
    # Legs and talons.
    for side in (-1, 1):
        x = 23.5 + side * 2.4
        p.poly(T([(x - 0.9, 17.0), (x + 0.9, 17.0), (x + side * 1.6 + 0.7, 21.0), (x + side * 1.6 - 0.7, 21.0)]), None,
               grad=(gold[0], gold[1]), outline=ink, width=w * 0.8)
        for c in (-1, 0, 1):
            cx = x + side * 1.6 + c * 0.9
            p.poly(T([(cx - 0.35, 20.8), (cx + 0.35, 20.8), (cx + c * 0.5 + 0.1, 22.6), (cx + c * 0.5 - 0.3, 22.2)]),
                   (200, 40, 30), outline=ink, width=w * 0.5)
    # Body with breast feathers.
    p.ellipse(T([(19.8, 7.0)])[0] + T([(27.2, 19.6)])[0], None, grad=(gold[0], gold[1]), outline=ink, width=w)
    for row in range(4):
        y = 10.5 + row * 2.1
        for k in range(-1, 2):
            x = 23.5 + k * 1.6 + (row % 2) * 0.8 - 0.4
            if abs(x - 23.5) < 3.2 - row * 0.3:
                p.line(T(arc(x, y, 0.8, 0.7, 20, 160, 6)), rgba(ink[:3], 150), 0.12 * s)
    # Neck and head in profile, looking left.
    p.poly(T([(21.0, 9.6), (20.0, 5.2), (23.6, 4.0), (25.2, 8.8)]), None, grad=(gold[0], gold[1]), outline=ink, width=w)
    p.ellipse(T([(19.0, 1.6)])[0] + T([(24.4, 6.8)])[0], None, grad=(gold[0], gold[1]), outline=ink, width=w)
    p.poly(T([(19.6, 3.6), (16.4, 4.6), (16.2, 6.0), (17.2, 5.6), (19.6, 5.8)]), (250, 160, 40), outline=ink, width=w * 0.8)
    p.circle(ox + 20.9 * s, oy + 3.7 * s, 0.6 * s, (200, 30, 20), outline=ink, width=w * 0.5)
    p.circle(ox + 20.75 * s, oy + 3.55 * s, 0.18 * s, (255, 255, 255), outline=None)
    p.line(T([(22.0, 4.2), (23.6, 3.4)]), rgba(ink[:3], 160), 0.15 * s)


def title_eagle():
    p = Pic(47, 27)
    p.glow(23.5, 13, 16, (255, 200, 80), 0.25)
    draw_eagle(p, 0, 0, 1.0)
    return p


def eagle_statue():
    p = Pic(12, 13)
    p.glow(6, 5, 6, (255, 210, 80), 0.3)
    p.poly([(2.6, 9.4), (9.4, 9.4), (8.8, 11.0), (3.2, 11.0)], None, grad=((250, 250, 245), (180, 180, 190)), width=0.18)
    p.rrect((1.8, 11.0, 10.2, 12.9), None, r=0.3, grad=((240, 240, 236), (160, 160, 172)), width=0.18)
    draw_eagle(p, 0.2, 0.3, 11.6 / 47)
    return p


# ------------------------------------------------------------------ objects

def key(colour):
    light, dark = COLOURS[colour]
    p = Pic(11, 5)
    g = (tuple(min(255, c + 40) for c in light), dark)
    p.rrect((3.6, 1.9, 10.7, 3.1), None, r=0.3, grad=g, width=0.18)
    for x0, y1 in [(7.3, 4.6), (8.7, 4.1), (9.9, 4.8)]:
        p.rrect((x0, 2.6, x0 + 0.8, y1), None, r=0.15, grad=g, width=0.16)
    p.ellipse((0.2, 0.4, 4.4, 4.6), None, grad=g, width=0.2)
    p.ellipse((1.3, 1.5, 3.3, 3.5), (0, 0, 0, 0), outline=INK, width=0.16)
    # Punch the hole through.
    hole = Image.new('L', p.im.size, 0)
    ImageDraw.Draw(hole).ellipse(p.B((1.45, 1.65, 3.15, 3.35)), fill=255)
    clear = Image.new('RGBA', p.im.size, (0, 0, 0, 0))
    p.im.paste(clear, (0, 0), hole)
    p.line([(4.0, 2.2), (10.2, 2.2)], (255, 255, 255, 140), 0.18)
    return p


def potion(colour):
    light, dark = COLOURS[colour]
    p = Pic(6, 8)
    p.glow(3, 5.4, 3.0, light, 0.35)
    p.rrect((2.4, 1.0, 3.6, 3.2), (220, 236, 245, 150), r=0.2, width=0.16)
    p.circle(3.0, 5.3, 2.6, (220, 236, 245, 110), width=0.18)
    # Liquid: the lower part of the bulb.
    layer = Pic(6, 8)
    layer.circle(3.0, 5.3, 2.35, None, grad=(light, dark), outline=None)
    cut = Image.new('L', p.im.size, 0)
    ImageDraw.Draw(cut).rectangle(p.B((0, 4.4, 6, 8)), fill=255)
    liquid = Image.new('RGBA', p.im.size, (0, 0, 0, 0))
    liquid.paste(layer.im, (0, 0), cut)
    p.im = Image.alpha_composite(p.im, liquid)
    p.line([(0.8, 4.4 + 0.02), (5.2, 4.4 + 0.02)], tuple(min(255, c + 60) for c in light), 0.15)
    p.line(arc(3.0, 5.3, 1.7, 1.7, 200, 250, 6), (255, 255, 255, 220), 0.3)
    p.rrect((2.2, 0.1, 3.8, 1.3), None, r=0.25, grad=((214, 170, 110), (150, 100, 60)), width=0.16)
    return p


def lamp():
    p = Pic(7, 8)
    p.glow(3.5, 4.4, 3.6, (255, 220, 120), 0.6)
    p.ellipse((2.6, 0.1, 4.4, 1.8), (0, 0, 0, 0), outline=INK, width=0.3)
    p.ellipse((2.6, 0.1, 4.4, 1.8), (0, 0, 0, 0), outline=GOLD_D, width=0.18)
    p.poly([(1.2, 2.4), (3.5, 1.2), (5.8, 2.4)], None, grad=(GOLD_L, GOLD_D), width=0.16)
    p.rrect((1.6, 2.3, 5.4, 6.6), None, r=0.2, grad=((255, 246, 190), (250, 170, 60)), width=0.18)
    p.poly([(3.5, 3.0), (4.2, 4.5), (3.5, 5.6), (2.8, 4.5)], (255, 255, 230), outline=(250, 150, 40), width=0.12)
    for x in (2.8, 4.2):
        p.line([(x, 2.4), (x, 6.5)], GOLD_D, 0.16)
    p.rrect((1.0, 6.5, 6.0, 7.8), None, r=0.3, grad=(GOLD_L, GOLD_D), width=0.18)
    return p


def cross():
    p = Pic(5, 8)
    p.glow(2.5, 3, 2.6, (255, 220, 120), 0.35)
    g = (GOLD_L, GOLD_D)
    p.poly([(1.8, 0.2), (3.2, 0.2), (3.0, 1.9), (4.8, 1.8), (4.8, 3.2), (3.0, 3.1), (3.2, 7.8), (1.8, 7.8), (2.0, 3.1),
            (0.2, 3.2), (0.2, 1.8), (2.0, 1.9)], None, grad=g, angle=45, outline=GOLD_INK, width=0.18)
    p.circle(2.5, 2.5, 0.55, (220, 30, 40), outline=GOLD_INK, width=0.1)
    p.circle(2.35, 2.35, 0.15, (255, 220, 220), outline=None)
    return p


def bread():
    p = Pic(9, 5)
    p.rrect((0.3, 0.8, 8.7, 4.8), None, r=2.0, grad=((236, 176, 90), (150, 86, 32)), width=0.2)
    for x in (2.2, 4.0, 5.8):
        p.line([(x, 1.4), (x + 1.2, 2.6)], (255, 222, 160), 0.3)
    return p


def coins():
    p = Pic(9, 5)
    g = (GOLD_L, GOLD_D)
    for (x, y) in [(4.2, 3.0), (4.4, 2.3), (4.1, 1.6), (4.3, 0.9)]:
        p.ellipse((x - 2.0, y - 0.7, x + 2.0, y + 0.9), None, grad=g, outline=GOLD_INK, width=0.15)
    p.ellipse((5.0, 2.8, 8.8, 4.8), None, grad=g, outline=GOLD_INK, width=0.15)
    p.ellipse((5.9, 3.3, 7.9, 4.3), (0, 0, 0, 0), outline=GOLD_D, width=0.12)
    return p


def goblet():
    p = Pic(7, 8)
    g = (GOLD_L, GOLD_D)
    p.poly(arc(3.5, 0.6, 3.1, 3.8, 0, 180, 20), None, grad=g, angle=0, outline=GOLD_INK, width=0.18)
    p.ellipse((0.4, 0.1, 6.6, 1.2), (130, 20, 30), outline=GOLD_INK, width=0.15)
    p.rrect((3.0, 4.2, 4.0, 6.6), None, r=0.2, grad=g, angle=0, width=0.15)
    p.ellipse((1.2, 6.3, 5.8, 7.9), None, grad=g, width=0.16)
    p.circle(2.2, 2.2, 0.35, (220, 40, 40), width=0.08)
    p.circle(3.5, 2.6, 0.4, (40, 200, 220), width=0.08)
    p.circle(4.8, 2.2, 0.35, (220, 40, 40), width=0.08)
    return p


def crown():
    p = Pic(10, 5)
    g = (GOLD_L, GOLD_D)
    pts = [(0.5, 4.8), (0.6, 1.4), (2.2, 2.8), (3.4, 0.6), (5.0, 2.4), (6.6, 0.6), (7.8, 2.8), (9.4, 1.4), (9.5, 4.8)]
    p.poly(pts, None, grad=g, outline=GOLD_INK, width=0.18)
    for x, y in [(0.6, 1.2), (3.4, 0.5), (6.6, 0.5), (9.4, 1.2)]:
        p.circle(x, y, 0.45, (250, 250, 240), outline=GOLD_INK, width=0.1)
    p.rrect((0.5, 3.4, 9.5, 4.8), None, r=0.2, grad=g, width=0.15)
    for x, c in [(2.2, (220, 30, 40)), (5.0, (40, 200, 220)), (7.8, (40, 180, 70))]:
        p.ellipse((x - 0.5, 3.6, x + 0.5, 4.6), c, outline=GOLD_INK, width=0.08)
    return p


def jewels(colour):
    light, dark = COLOURS[colour]
    p = Pic(10, 6)
    p.line(arc(5, 1.5, 4.2, 4.0, 10, 170, 20), GOLD, 0.25)
    for cx, cy, r in [(2.6, 3.6, 1.8), (7.4, 3.6, 1.8), (5.0, 3.8, 2.1)]:
        gem = [(cx - r, cy - 0.3 * r), (cx - 0.5 * r, cy - r), (cx + 0.5 * r, cy - r), (cx + r, cy - 0.3 * r), (cx, cy + r)]
        p.poly(gem, None, grad=(tuple(min(255, c + 50) for c in light), dark), angle=60, width=0.15)
        p.line([(cx - r, cy - 0.3 * r), (cx + r, cy - 0.3 * r)], (255, 255, 255, 160), 0.1)
        p.line([(cx - 0.5 * r, cy - r), (cx, cy + r), (cx + 0.5 * r, cy - r)], (255, 255, 255, 110), 0.08)
        p.circle(cx - 0.3 * r, cy - 0.55 * r, 0.22, (255, 255, 255), outline=None)
    return p


# ------------------------------------------------------------------ scenery

def torch(flicker):
    p = Pic(5, 11)
    p.glow(2.5, 2.2, 2.6, (255, 170, 50), 0.6)
    p.poly([(2.0, 4.6), (3.0, 4.6), (2.8, 10.6), (2.2, 10.6)], None, grad=(WOOD_L, WOOD_D), angle=0, width=0.15)
    p.poly([(0.8, 3.6), (4.2, 3.6), (3.4, 5.0), (1.6, 5.0)], None, grad=((120, 120, 130), (50, 50, 60)), width=0.15)
    lean = 0.5 if flicker else -0.3
    p.poly([(1.0, 3.7), (1.2, 2.2), (2.5 + lean, 0.1), (3.8, 2.2), (4.0, 3.7)], (240, 90, 30), outline=None)
    p.poly([(1.7, 3.7), (1.9, 2.6), (2.5 + lean * 0.6, 1.1), (3.1, 2.6), (3.3, 3.7)], (255, 200, 60), outline=None)
    p.poly([(2.2, 3.7), (2.5 + lean * 0.3, 2.3), (2.8, 3.7)], (255, 250, 220), outline=None)
    return p


def window():
    p = Pic(10, 13)
    outer = [(0.2, 12.8), (0.2, 4.8)] + arc(5, 4.8, 4.8, 4.6, 180, 360, 20) + [(9.8, 12.8)]
    p.poly(outer, None, grad=((200, 196, 186), (130, 126, 120)), width=0.2)
    inner = [(1.3, 11.6), (1.3, 5.0)] + arc(5, 5.0, 3.7, 3.6, 180, 360, 20) + [(8.7, 11.6)]
    p.poly(inner, None, grad=((40, 70, 170), (150, 200, 250)), width=0.2)
    p.ellipse((2.2, 6.6, 5.0, 7.6), (255, 255, 255, 150), outline=None)
    p.ellipse((5.0, 8.8, 8.0, 9.8), (255, 255, 255, 120), outline=None)
    p.circle(6.6, 3.6, 0.7, (255, 255, 220), outline=None)
    p.line([(5.0, 1.4), (5.0, 11.6)], (60, 60, 70), 0.35)
    p.line([(1.3, 7.2), (8.7, 7.2)], (60, 60, 70), 0.35)
    p.rrect((0.0, 11.6, 10.0, 12.9), None, r=0.2, grad=((210, 206, 196), (120, 116, 110)), width=0.18)
    return p


def shield():
    p = Pic(8, 9)
    outline = [(0.3, 0.3), (7.7, 0.3), (7.7, 4.0)] + arc(4.0, 4.0, 3.7, 4.7, 0, 90, 10)[1:] + arc(4.0, 4.0, 3.7, 4.7, 90, 180, 10)[1:] + [(0.3, 0.3)]
    p.poly(outline, None, grad=((236, 70, 60), (140, 20, 24)), outline=GOLD_INK, width=0.45)
    p.poly([(0.6, 5.0), (4.0, 2.2), (7.4, 5.0), (7.2, 6.4), (4.0, 3.8), (0.8, 6.4)], (250, 248, 240), width=0.15)
    p.stroke('poly', outline, GOLD, 0.25)
    p.circle(4.0, 1.4, 0.5, GOLD, width=0.1)
    return p


def tapestry():
    p = Pic(10, 15)
    p.rrect((0.0, 0.0, 10.0, 0.8), None, r=0.3, grad=(GOLD_L, GOLD_D), width=0.15)
    body = [(0.5, 0.6), (9.5, 0.6), (9.5, 14.0), (5.0, 12.8), (0.5, 14.0)]
    p.poly(body, None, grad=((210, 40, 40), (110, 14, 20)), width=0.2)
    p.stroke('poly', [(1.1, 1.2), (8.9, 1.2), (8.9, 13.0), (5.0, 12.0), (1.1, 13.0)], GOLD, 0.2)
    draw_eagle(p, 1.4, 4.0, 7.2 / 47, gold=(GOLD_L, GOLD_D), ink=(120, 70, 10, 255))
    for x in [1.0 + i * 0.8 for i in range(11)]:
        y0 = 14.0 - (x - 0.5) / 4.5 * 1.2 if x < 5 else 12.8 + (x - 5.0) / 4.5 * 1.2
        p.line([(x, y0), (x, y0 + 0.9)], GOLD, 0.15)
    return p


def books():
    p = Pic(16, 14)
    p.rrect((0.2, 0.2, 15.8, 13.8), None, r=0.3, grad=(WOOD_L, WOOD_D), width=0.2)
    import random
    rnd = random.Random(4)
    shelves = [(1.0, 4.3), (5.0, 8.3), (9.0, 12.4)]
    palette = [(170, 40, 40), (40, 110, 60), (50, 70, 150), (160, 120, 40), (110, 40, 110), (40, 120, 130), (90, 60, 40)]
    for top, bottom in shelves:
        p.rrect((0.8, top - 0.3, 15.2, bottom + 0.1), (40, 26, 16), r=0.1, outline=None)
        x = 1.0
        while x < 14.6:
            w = rnd.uniform(0.55, 1.1)
            h = rnd.uniform(0.7, 1.0) * (bottom - top)
            c = rnd.choice(palette)
            lean = 0.0 if rnd.random() > 0.15 else 0.4
            p.poly([(x, bottom), (x + w, bottom), (x + w + lean, bottom - h), (x + lean, bottom - h)], None,
                   grad=(tuple(min(255, v + 50) for v in c), c), angle=0, width=0.1)
            p.line([(x + 0.15 + lean * 0.5, bottom - h * 0.75), (x + w - 0.15 + lean * 0.5, bottom - h * 0.75)], GOLD, 0.1)
            x += w + 0.05
        p.rrect((0.6, bottom, 15.4, bottom + 0.6), None, r=0.1, grad=(WOOD_L, WOOD_D), width=0.12)
    return p


def barrels():
    p = Pic(16, 9)
    for x0 in (0.3, 8.3):
        p.rrect((x0, 0.3, x0 + 7.2, 8.8), None, r=2.0, grad=((180, 120, 60), (90, 52, 22)), angle=0, width=0.2)
        for k in range(1, 5):
            x = x0 + k * 1.44
            p.line([(x, 0.5), (x - 0.1 * (k - 2.5), 4.5), (x, 8.6)], (80, 46, 20, 200), 0.1)
        for y in (1.6, 7.4):
            p.rrect((x0 + 0.1, y - 0.35, x0 + 7.1, y + 0.35), IRON, r=0.2, width=0.1)
        p.ellipse((x0 + 0.6, 0.0, x0 + 6.6, 1.2), (150, 100, 50), width=0.15)
    return p


def altar():
    p = Pic(16, 10)
    p.rrect((1.0, 4.4, 15.0, 9.8), None, r=0.2, grad=((200, 196, 186), (120, 116, 110)), width=0.2)
    p.poly([(0.6, 4.2), (15.4, 4.2), (15.0, 7.6), (1.0, 7.6)], (246, 244, 236), width=0.18)
    p.poly([(6.6, 4.2), (9.4, 4.2), (9.4, 8.6), (8.0, 8.0), (6.6, 8.6)], (180, 30, 36), width=0.15)
    p.line([(8.0, 4.8), (8.0, 7.2)], GOLD, 0.3)
    p.line([(7.3, 5.6), (8.7, 5.6)], GOLD, 0.3)
    for x in (2.5, 13.5):
        p.glow(x, 0.9, 1.3, (255, 200, 80), 0.6)
        p.rrect((x - 0.4, 1.6, x + 0.4, 4.2), (250, 248, 230), r=0.1, width=0.12)
        p.poly([(x - 0.3, 1.6), (x, 0.3), (x + 0.3, 1.6)], (255, 190, 60), outline=None)
        p.rrect((x - 0.9, 3.8, x + 0.9, 4.3), GOLD, r=0.1, width=0.1)
    return p


def throne():
    p = Pic(12, 14)
    g = (GOLD_L, GOLD_D)
    p.poly([(1.4, 9.0), (1.4, 2.0), (3.6, 1.0), (6.0, 0.1), (8.4, 1.0), (10.6, 2.0), (10.6, 9.0)], None, grad=g, width=0.2)
    p.poly([(2.4, 8.6), (2.4, 2.6), (6.0, 1.2), (9.6, 2.6), (9.6, 8.6)], None, grad=((210, 40, 46), (110, 14, 22)), width=0.15)
    draw_eagle(p, 3.0, 3.0, 6.0 / 47, ink=(120, 70, 10, 255))
    p.rrect((0.6, 8.4, 11.4, 10.4), None, r=0.4, grad=((200, 40, 46), (110, 14, 22)), width=0.2)
    p.rrect((0.4, 10.2, 11.6, 11.2), None, r=0.2, grad=g, width=0.18)
    for x in (1.0, 10.0):
        p.rrect((x, 11.0, x + 1.0, 14.0), None, r=0.2, grad=g, angle=0, width=0.16)
    for x in (1.4, 10.6):
        p.circle(x, 1.8, 0.6, GOLD_L, outline=GOLD_INK, width=0.1)
    return p


def bed():
    p = Pic(20, 8)
    p.rrect((0.2, 0.2, 2.2, 8.0), None, r=0.4, grad=(WOOD_L, WOOD_D), angle=0, width=0.2)
    p.rrect((17.8, 3.0, 19.8, 8.0), None, r=0.4, grad=(WOOD_L, WOOD_D), angle=0, width=0.2)
    p.rrect((1.8, 4.6, 18.2, 6.2), None, r=0.2, grad=(WOOD_L, WOOD_D), width=0.18)
    p.rrect((2.0, 2.4, 6.0, 4.4), (250, 248, 240), r=0.8, width=0.15)
    p.poly([(4.8, 3.2), (18.0, 3.2), (18.2, 5.6), (4.6, 5.6)], None, grad=((190, 70, 170), (100, 24, 100)), width=0.18)
    p.line([(5.0, 3.6), (17.8, 3.6)], (250, 220, 250), 0.2)
    for x in (7.0, 10.0, 13.0, 16.0):
        p.line([(x, 3.8), (x - 0.4, 5.4)], (90, 20, 90, 160), 0.1)
    return p


def chains():
    p = Pic(6, 13)
    for i in range(7):
        y = 0.2 + i * 1.3
        if i % 2 == 0:
            p.ellipse((1.9, y, 3.1, y + 1.7), (0, 0, 0, 0), outline=(150, 150, 160), width=0.32)
        else:
            p.rrect((2.35, y, 2.65, y + 1.7), (120, 120, 130), r=0.1, width=0.1)
    p.ellipse((0.6, 9.4, 4.4, 12.8), (0, 0, 0, 0), outline=INK, width=0.6)
    p.ellipse((0.6, 9.4, 4.4, 12.8), (0, 0, 0, 0), outline=(150, 150, 160), width=0.4)
    return p


def cobweb():
    p = Pic(10, 10)
    c = (240, 240, 250, 200)
    rays = [(10, 0.2), (9.6, 3.0), (8.2, 6.4), (6.0, 8.6), (3.0, 9.8), (0.2, 10)]
    for x, y in rays:
        p.line([(0, 0), (x, y)], c, 0.1)
    for r in (2.0, 4.0, 6.0, 8.0):
        pts = [(x * r / 10, y * r / 10) for x, y in rays]
        sag = []
        for a, b in zip(pts, pts[1:]):
            mid = ((a[0] + b[0]) / 2 * 0.92, (a[1] + b[1]) / 2 * 0.92)
            sag += [a, mid]
        sag.append(pts[-1])
        p.line(sag, c, 0.08)
    return p


def bones():
    p = Pic(12, 6)
    bone = (236, 230, 210)
    for a, b in [((5.2, 5.4), (11.6, 1.6)), ((5.4, 1.6), (11.4, 5.6))]:
        p.line([a, b], INK, 0.9)
        p.line([a, b], bone, 0.6)
        for x, y in (a, b):
            p.circle(x, y - 0.3, 0.45, bone, width=0.1)
            p.circle(x, y + 0.3, 0.45, bone, width=0.1)
    p.ellipse((0.4, 0.3, 4.8, 4.6), None, grad=((250, 246, 230), (180, 170, 150)), width=0.18)
    p.rrect((1.3, 3.8, 3.9, 5.6), (230, 224, 204), r=0.3, width=0.15)
    p.ellipse((1.0, 1.8, 2.3, 3.2), (30, 20, 20), outline=None)
    p.ellipse((2.9, 1.8, 4.2, 3.2), (30, 20, 20), outline=None)
    for x in (1.9, 2.6, 3.3):
        p.line([(x, 4.2), (x, 5.4)], (60, 50, 40), 0.1)
    return p


def cauldron():
    p = Pic(12, 10)
    p.glow(6, 9, 3.5, (255, 140, 40), 0.6)
    for x0, x1 in [(2.5, 5.0), (7.0, 9.5)]:
        p.line([(x0, 9.6), (x1, 8.8)], WOOD_D, 0.6)
    p.poly([(3.6, 9.8), (4.6, 7.8), (5.6, 9.2), (6.4, 7.4), (7.4, 9.0), (8.4, 7.8), (8.6, 9.8)], (250, 140, 40), outline=None)
    p.ellipse((0.8, 2.0, 11.2, 9.0), None, grad=((90, 90, 100), (20, 20, 26)), width=0.22)
    p.ellipse((0.6, 1.4, 11.4, 3.4), (50, 50, 58), width=0.2)
    p.ellipse((1.4, 1.8, 10.6, 3.0), None, grad=((140, 250, 110), (40, 150, 40)), angle=0, outline=None)
    for x, y, r in [(3.6, 1.4, 0.5), (6.6, 0.9, 0.7), (8.6, 1.5, 0.4)]:
        p.circle(x, y, r, (170, 255, 140, 220), outline=(40, 120, 40), width=0.08)
    return p


def table():
    p = Pic(16, 8)
    for x in (1.2, 14.0):
        p.rrect((x, 2.6, x + 0.8, 8.0), None, r=0.2, grad=(WOOD_L, WOOD_D), angle=0, width=0.15)
    p.rrect((0.2, 2.0, 15.8, 3.2), None, r=0.3, grad=(WOOD_L, WOOD_D), width=0.18)
    p.glow(3.0, 0.4, 1.0, (255, 200, 80), 0.6)
    p.rrect((2.6, 0.8, 3.4, 2.0), (250, 248, 230), r=0.1, width=0.1)
    p.poly([(2.75, 0.8), (3.0, 0.0), (3.25, 0.8)], (255, 190, 60), outline=None)
    p.rrect((8.0, 0.4, 9.2, 2.0), None, r=0.4, grad=((80, 140, 90), (30, 70, 40)), width=0.12)
    p.rrect((8.35, -0.2, 8.85, 0.6), (60, 100, 70), r=0.1, width=0.08)
    p.ellipse((10.8, 1.3, 13.4, 2.1), (210, 200, 180), width=0.1)
    p.ellipse((11.4, 0.6, 12.8, 1.7), (236, 176, 90), width=0.1)
    return p


def portrait():
    p = Pic(10, 11)
    p.rrect((0.0, 0.0, 10.0, 11.0), None, r=0.2, grad=(GOLD_L, GOLD_D), angle=45, width=0.2)
    p.rrect((1.0, 1.0, 9.0, 10.0), None, r=0.1, grad=((50, 60, 110), (14, 16, 40)), width=0.15)
    p.poly([(2.0, 10.0), (2.6, 7.4), (5.0, 6.6), (7.4, 7.4), (8.0, 10.0)], None, grad=((200, 40, 46), (110, 14, 22)), width=0.12)
    p.poly([(4.4, 7.0), (5.0, 8.0), (5.6, 7.0)], (250, 248, 240), width=0.08)
    p.ellipse((3.6, 2.6, 6.4, 6.4), None, grad=(SKIN, SKIN_D), width=0.12)
    p.poly([(3.4, 3.4), (4.2, 1.8), (5.0, 2.4), (5.8, 1.8), (6.6, 3.4), (6.4, 4.4), (3.6, 4.4)], (70, 40, 20), width=0.1)
    p.circle(4.4, 4.6, 0.2, INK, outline=None)
    p.circle(5.6, 4.6, 0.2, INK, outline=None)
    p.line([(4.6, 5.6), (5.4, 5.6)], (150, 60, 50), 0.12)
    return p


def main():
    jobs = {
        'HeroFront0': lambda: hero_front(False), 'HeroFront1': lambda: hero_front(True),
        'HeroBack0': lambda: hero_front(False, back=True), 'HeroBack1': lambda: hero_front(True, back=True),
        'HeroSide0': lambda: hero_side(False), 'HeroSide1': lambda: hero_side(True),
        'Ghost0': lambda: ghost(0), 'Ghost1': lambda: ghost(1),
        'Bat0': lambda: bat(True), 'Bat1': lambda: bat(False),
        'Knight0': lambda: knight(False), 'Knight1': lambda: knight(True),
        'Lamp': lamp, 'Cross': cross, 'Bread': bread, 'Eagle': eagle_statue, 'Coins': coins, 'Goblet': goblet,
        'Crown': crown, 'Torch': lambda: torch(False), 'TorchFlicker': lambda: torch(True), 'Window': window,
        'Shield': shield, 'Tapestry': tapestry, 'Books': books, 'Barrels': barrels, 'Altar': altar, 'Throne': throne,
        'Bed': bed, 'Chains': chains, 'Cobweb': cobweb, 'Bones': bones, 'Cauldron': cauldron, 'Table': table,
        'Portrait': portrait, 'TitleEagle': title_eagle,
    }
    for c in ('Red', 'Green', 'Blue', 'Yellow'):
        jobs['Key-' + c] = (lambda c=c: key(c))
    for c in ('Cyan', 'Magenta', 'Green', 'Red', 'Yellow', 'Blue'):
        jobs['Potion-' + c] = (lambda c=c: potion(c))
    for c in ('Magenta', 'Cyan', 'Red', 'Green', 'Blue', 'Yellow'):
        jobs['Jewels-' + c] = (lambda c=c: jewels(c))
    for name, make in jobs.items():
        make().save(name)
    print(f'{len(jobs)} pictures in {OUT}')


if __name__ == '__main__':
    main()
