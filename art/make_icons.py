"""Generates every platform icon from the game's golden eagle (run from the repo root).

The eagle is the game's vector title eagle (tools/make_art.py), in gold on a night-blue ground.
"""
import os, re, subprocess, shutil, sys
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, 'tools')
import make_art  # noqa: E402


def eagle_image():
    """The title eagle drawn as vector art at 48 pixels per unit (about 2250 pixels wide)."""
    make_art.PX = 48
    pic = make_art.Pic(47, 27)
    make_art.draw_eagle(pic, 0, 0, 1.0)
    return pic.im.resize((47 * 48, 27 * 48), Image.LANCZOS)


EAGLE = eagle_image()


def icon(size, pad=0.12, wide=None, rounded=False):
    w, h = wide or (size, size)
    s = 4  # supersample
    W, H = w * s, h * s
    bg = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(bg)
    for y in range(H):
        t = y / max(1, H - 1)
        d.line([(0, y), (W, y)], fill=(int(18 + 20 * t), int(22 + 10 * t), int(70 - 30 * t), 255))
    glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    r = int(min(W, H) * 0.42)
    gd.ellipse([W // 2 - r, H // 2 - r, W // 2 + r, H // 2 + r], fill=(255, 190, 60, 90))
    bg = Image.alpha_composite(bg, glow.filter(ImageFilter.GaussianBlur(min(W, H) * 0.08)))
    avail_w, avail_h = W * (1 - 2 * pad), H * (1 - 2 * pad)
    k = min(avail_w / EAGLE.width, avail_h / EAGLE.height)
    e = EAGLE.resize((int(EAGLE.width * k), int(EAGLE.height * k)), Image.LANCZOS)
    shadow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ex, ey = (W - e.width) // 2, (H - e.height) // 2
    shadow.paste((0, 0, 0, 160), (ex + W // 80, ey + H // 60), e)
    bg = Image.alpha_composite(bg, shadow.filter(ImageFilter.GaussianBlur(min(W, H) * 0.01)))
    bg.alpha_composite(e, (ex, ey))
    return bg.resize((w, h), Image.LANCZOS)


def save(im, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)


# Android launcher icons and splash
for folder, px in [('mdpi', 48), ('hdpi', 72), ('xhdpi', 96), ('xxhdpi', 144), ('xxxhdpi', 192)]:
    save(icon(px, 0.1), f'GoldenEagle.Android/Resources/drawable-{folder}/icon.png')
    save(icon(px * 2, 0.3), f'GoldenEagle.Android/Resources/drawable-{folder}/splash.png')

# iOS app icon set (opaque)
ios = 'GoldenEagle.iOS/AppIcon.xcassets/AppIcon.appiconset'
for n in [20, 29, 40, 58, 60, 76, 80, 87, 120, 152, 167, 180, 1024]:
    icon(n, 0.1).convert('RGB').save(os.path.join(ios, f'icon_{n}x{n}.png'))

# Windows .ico and MSIX logos
os.makedirs('GoldenEagle.WindowsDX/Windows/Assets', exist_ok=True)
icon(256, 0.06).save('GoldenEagle.WindowsDX/Icon.ico', sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
shutil.copy('GoldenEagle.WindowsDX/Icon.ico', 'GoldenEagle.DesktopGL/Icon.ico')
for name, size in [('Square44x44Logo', (44, 44)), ('Square150x150Logo', (150, 150)), ('StoreLogo', (50, 50)),
                   ('Wide310x150Logo', (310, 150)), ('SplashScreen', (620, 300)), ('Square71x71Logo', (71, 71)),
                   ('Square310x310Logo', (310, 310))]:
    icon(min(size), 0.08, wide=size).save(f'GoldenEagle.WindowsDX/Windows/Assets/{name}.png')
icon(44, 0.0).save('GoldenEagle.WindowsDX/Windows/Assets/Square44x44Logo.targetsize-44_altform-unplated.png')

# macOS .icns
iconset = 'art/GoldenEagle.iconset'
os.makedirs(iconset, exist_ok=True)
for s in [16, 32, 128, 256, 512]:
    icon(s, 0.1).save(f'{iconset}/icon_{s}x{s}.png')
    icon(s * 2, 0.1).save(f'{iconset}/icon_{s}x{s}@2x.png')
subprocess.run(['iconutil', '-c', 'icns', iconset, '-o', 'GoldenEagle.DesktopGL/GoldenEagle.icns'], check=True)
shutil.rmtree(iconset)
icon(1024, 0.1).convert('RGB').save('art/icon-1024.png')
print('icons done')
