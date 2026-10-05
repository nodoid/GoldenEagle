#!/usr/bin/env python3
"""Builds the store screenshots, graphics and app preview videos in store/ from game captures.

    python3 tools/make_store_assets.py stills <capture-root>
    python3 tools/make_store_assets.py video <capture-root> <en|fr> <mobile|desktop>

<capture-root> holds the output of the game's capture mode (see tools/capture_all.sh):

    <lang>/desktop/stills/*.png     macOS captures with desktop prompts   (2400 x 2240)
    <lang>/mobile/stills/*.png      captures with touch prompts
    <lang>/windows/stills/*.png     captures made by the Windows build (tools/build_windows.sh capture)
    <lang>/video-<layout>/video/NN  frame folders + NN.wav from `videos`

The game screen keeps its proportions; the rest of each image is the same picture enlarged,
blurred and darkened, so nothing is stretched and there are no black bars. Videos are also
copied to ~/Movies/Golden Eagle.
"""
import glob
import os
import shutil
import subprocess
import sys
import wave

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STORE = os.path.join(ROOT, 'store')
MOVIES = os.path.join(os.path.expanduser('~'), 'Movies', 'Golden Eagle')
LANGS = {'en': 'English', 'fr': 'French'}

# The screens used for every store, in listing order (Google Play takes the first eight).
SHOTS = ['07-eyrie', '02-title', '05-throne', '06-catacombs', '04-chapel', '13-guardroom',
         '11-laboratory', '03-dungeon', '09-victory', '12-instructions']

# Video segments in montage order; every captured frame is used (50 per second, ~29 s).
SEGMENTS = ['01', '02', '03', '04', '05', '06', '07', '08', '09']


def fit(img, w, h):
    """The screen fitted into w x h over a blurred, darkened copy of itself."""
    img = img.convert('RGB')
    cover = max(w / img.width, h / img.height)
    bg = img.resize((int(img.width * cover) + 1, int(img.height * cover) + 1), Image.BILINEAR)
    bg = bg.crop(((bg.width - w) // 2, (bg.height - h) // 2, (bg.width - w) // 2 + w, (bg.height - h) // 2 + h))
    bg = bg.resize((max(1, w // 8), max(1, h // 8)), Image.BILINEAR).filter(ImageFilter.GaussianBlur(6)).resize((w, h), Image.BICUBIC)
    bg = ImageEnhance.Brightness(bg).enhance(0.45)
    s = min(w / img.width, h / img.height)
    fg = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
    shadow = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rectangle([(w - fg.width) // 2 - 6, (h - fg.height) // 2 - 6,
                                      (w + fg.width) // 2 + 6, (h + fg.height) // 2 + 6], fill=(0, 0, 0, 170))
    out = Image.alpha_composite(bg.convert('RGBA'), shadow.filter(ImageFilter.GaussianBlur(max(w, h) // 120)))
    out.paste(fg, ((w - fg.width) // 2, (h - fg.height) // 2))
    return out.convert('RGB')


def screenshots(src, dest, w, h, count=len(SHOTS)):
    if not os.path.isdir(src):
        print(f'skipped {dest}: no captures in {src}')
        return
    os.makedirs(dest, exist_ok=True)
    for i, name in enumerate(SHOTS[:count]):
        fit(Image.open(os.path.join(src, name + '.png')), w, h).save(os.path.join(dest, f'{i + 1:02d}-{name[3:]}.png'))
    print(f'{os.path.relpath(dest, ROOT)}: {count} x {w}x{h}')


def night(w, h, eagle_box=None, seed=1984):
    """A starry night with the golden eagle (for art that must not contain text)."""
    import random
    sys.path.insert(0, os.path.join(ROOT, 'tools'))
    import make_art
    img = Image.new('RGB', (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / h
        d.line([(0, y), (w, y)], fill=(int(10 + 30 * t), int(12 + 38 * t), int(40 + 80 * t)))
    rnd = random.Random(seed)
    for _ in range(int(w * h / 9000)):
        x, y, r = rnd.random() * w, rnd.random() * h * 0.8, rnd.choice([1, 1, 2, 2, 3]) * max(1, w // 1920)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255))
    glow = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    bx, by, bw = eagle_box
    ImageDraw.Draw(glow).ellipse([bx + bw * 0.1, by - bw * 0.05, bx + bw * 0.9, by + bw * 0.55], fill=(255, 190, 60, 110))
    img = Image.alpha_composite(img.convert('RGBA'), glow.filter(ImageFilter.GaussianBlur(bw * 0.12)))
    make_art.PX = max(8, int(bw / 47))
    pic = make_art.Pic(47, 27)
    make_art.draw_eagle(pic, 0, 0, 1.0)
    eagle = pic.im.resize((int(bw), int(bw * 27 / 47)), Image.LANCZOS)
    img.alpha_composite(eagle, (int(bx), int(by)))
    return img.convert('RGB')


def stills(cap):
    icon = Image.open(os.path.join(ROOT, 'art', 'icon-1024.png')).convert('RGB')
    for lang in LANGS:
        desktop = os.path.join(cap, lang, 'desktop', 'stills')
        mobile = os.path.join(cap, lang, 'mobile', 'stills')
        windows = os.path.join(cap, lang, 'windows', 'stills')
        out = os.path.join(STORE, lang)

        # Apple App Store (iOS): landscape iPhone 6.9" and 6.5", iPad 13".
        a = os.path.join(out, 'app-store')
        screenshots(mobile, os.path.join(a, 'iphone-6.9in-2868x1320'), 2868, 1320)
        screenshots(mobile, os.path.join(a, 'iphone-6.5in-2688x1242'), 2688, 1242)
        screenshots(mobile, os.path.join(a, 'ipad-13in-2752x2064'), 2752, 2064)
        icon.save(os.path.join(a, 'icon-1024.png'))

        # Mac App Store.
        m = os.path.join(out, 'mac-app-store')
        screenshots(desktop, os.path.join(m, 'screenshots-2880x1800'), 2880, 1800)
        screenshots(desktop, os.path.join(m, 'screenshots-1440x900'), 1440, 900)

        # Google Play: phone and 10" tablet, icon and feature graphic.
        g = os.path.join(out, 'google-play')
        screenshots(mobile, os.path.join(g, 'phone-screenshots-1920x1080'), 1920, 1080, 8)
        screenshots(mobile, os.path.join(g, 'tablet-screenshots-2560x1600'), 2560, 1600, 8)
        icon.resize((512, 512), Image.LANCZOS).save(os.path.join(g, 'icon-512.png'))
        title = Image.open(os.path.join(desktop, '02-title.png')).convert('RGB')
        crop = title.crop((0, 0, title.width, title.height * 125 // 224))
        fit(crop, 1024, 500).save(os.path.join(g, 'feature-graphic-1024x500.png'))

        # Microsoft Store: captured by the Windows (DirectX) build when available.
        ms = os.path.join(out, 'microsoft-store')
        src = windows if os.path.isdir(windows) else desktop
        screenshots(src, os.path.join(ms, 'screenshots-3840x2160'), 3840, 2160)
        screenshots(src, os.path.join(ms, 'screenshots-1920x1080'), 1920, 1080)
        screenshots(src, os.path.join(ms, 'screenshots-1366x768'), 1366, 768)
        fit(Image.open(os.path.join(src, '02-title.png')), 2160, 2160).save(os.path.join(ms, 'box-art-2160x2160.png'))
        fit(Image.open(os.path.join(src, '02-title.png')), 1440, 2160).save(os.path.join(ms, 'poster-art-1440x2160.png'))
        icon.resize((300, 300), Image.LANCZOS).save(os.path.join(ms, 'store-logo-300x300.png'))
        # Super hero art must carry no text: the eagle over the night sky, off to the right.
        for w, h in [(1920, 1080), (3840, 2160)]:
            night(w, h, (w * 0.5, h * 0.18, w * 0.42)).save(os.path.join(ms, f'super-hero-art-{w}x{h}.png'))
    print('stills done')


def montage(video_root, tmp):
    """Joins every frame of each segment, and their audio, into one sequence."""
    frames = os.path.join(tmp, 'frames')
    shutil.rmtree(tmp, ignore_errors=True)
    os.makedirs(frames)
    n = 0
    audio = bytearray()
    rate = 22050
    for seg in SEGMENTS:
        files = sorted(glob.glob(os.path.join(video_root, seg, 'f*.png')))
        for f in files:
            os.link(f, os.path.join(frames, f'{n:05d}.png'))
            n += 1
        with wave.open(os.path.join(video_root, seg + '.wav')) as wv:
            rate = wv.getframerate()
            pcm = wv.readframes(wv.getnframes())
        want = len(files) * rate // 50 * 2
        audio += pcm[:want] + bytes(max(0, want - len(pcm)))
    wav = os.path.join(tmp, 'audio.wav')
    with wave.open(wav, 'wb') as wv:
        wv.setnchannels(1)
        wv.setsampwidth(2)
        wv.setframerate(rate)
        wv.writeframes(bytes(audio))
    return frames, wav, n


def encode(frames, wav, out, w, h):
    """H.264 / AAC at 30 fps, as App Store Connect wants; blurred fill around the screen."""
    os.makedirs(os.path.dirname(out), exist_ok=True)
    vf = (f'[0:v]split[a][b];'
          f'[a]scale={w // 8}:{h // 8}:force_original_aspect_ratio=increase,crop={w // 8}:{h // 8},boxblur=6:2,'
          f'scale={w}:{h},eq=brightness=-0.22:saturation=0.8[bg];'
          f'[b]scale=-2:{h}:flags=lanczos[fg];'
          f'[bg][fg]overlay=(W-w)/2:(H-h)/2,fps=30,format=yuv420p[v]')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', '50', '-i', os.path.join(frames, '%05d.png'),
                    '-i', wav, '-filter_complex', vf, '-map', '[v]', '-map', '1:a',
                    '-c:v', 'libx264', '-profile:v', 'high', '-level', '4.0', '-crf', '17', '-preset', 'slow',
                    '-c:a', 'aac', '-b:a', '256k', '-ar', '44100', '-ac', '2',
                    '-movflags', '+faststart', '-shortest', out], check=True)
    print('video', os.path.relpath(out, ROOT))


def video(cap, lang, layout):
    root = os.path.join(cap, lang, f'video-{layout}', 'video')
    tmp = os.path.join(cap, '_montage')
    frames, wav, n = montage(root, tmp)
    print(f'{lang} {layout}: {n} frames, {n / 50:.1f} s')
    os.makedirs(MOVIES, exist_ok=True)
    name = LANGS[lang]
    if layout == 'mobile':
        targets = [(os.path.join(STORE, lang, 'app-store', 'app-preview-iphone-1920x886.mp4'), 1920, 886,
                    f'Golden Eagle - iPhone app preview 1920x886 - {name}.mp4'),
                   (os.path.join(STORE, lang, 'app-store', 'app-preview-ipad-1600x1200.mp4'), 1600, 1200,
                    f'Golden Eagle - iPad app preview 1600x1200 - {name}.mp4')]
    else:
        targets = [(os.path.join(STORE, lang, 'mac-app-store', 'app-preview-1920x1080.mp4'), 1920, 1080,
                    f'Golden Eagle - Mac app preview 1920x1080 - {name}.mp4')]
    for out, w, h, movie in targets:
        encode(frames, wav, out, w, h)
        shutil.copy(out, os.path.join(MOVIES, movie))
    shutil.rmtree(tmp, ignore_errors=True)


if __name__ == '__main__':
    if len(sys.argv) >= 3 and sys.argv[1] == 'stills':
        stills(sys.argv[2])
    elif len(sys.argv) == 5 and sys.argv[1] == 'video':
        video(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        sys.exit(__doc__)
