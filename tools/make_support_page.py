#!/usr/bin/env python3
"""Builds the support page (with the privacy policy) for the store "Support URL".

    python3 tools/make_support_page.py

Writes docs/GoldenEagle-Support.html (self-contained: the icon and a screenshot are embedded)
and copies it to ~/Downloads, ready to upload to the web site. The page is in English (the
game itself and the store listings are also in French).
"""
import base64
import io
import os
import shutil

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'docs', 'GoldenEagle-Support.html')
EMAIL = 'paul@all-the-johnsons.co.uk'


def jpeg(path, width, quality=82):
    im = Image.open(path).convert('RGB')
    im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    buf = io.BytesIO()
    im.save(buf, 'JPEG', quality=quality, optimize=True, progressive=True)
    return 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()


def shot(name, lang):
    p = os.path.join(ROOT, 'store', lang, 'mac-app-store', 'screenshots-1440x900', name + '.png')
    return p if os.path.exists(p) else None


def main():
    icon = jpeg(os.path.join(ROOT, 'art', 'icon-1024.png'), 160)
    hero_path = shot('01-eyrie', 'en')
    hero_html = f'<img class="hero" src="{jpeg(hero_path, 1080)}" alt="Golden Eagle: the eagle\'s eyrie">' if hero_path else ''
    pics = [p for p in (shot('03-throne', 'en'), shot('04-catacombs', 'en'), shot('05-chapel', 'en')) if p]
    gallery_html = ('<div class="gallery">' + ''.join(f'<img src="{jpeg(p, 520, 78)}" alt="">' for p in pics) + '</div>') if pics else ''

    sections = [
        ('about', 'The game', '''<p>A treacherous lord has stolen the Golden Eagle, the magic statuette that protects the kingdom, and hidden it at the top of his castle. You enter alone, by the gatehouse. Find the eagle and carry it back out through the gate.</p>
<p>Golden Eagle is a new, high-resolution remake of <em>L'Aigle d'Or</em>, the 1984 Oric adventure by Louis-Marie Rocques (Loriciels). Each room is seen in perspective through its open front wall, as in the original. It runs on iPhone, iPad, Android, Mac and Windows, in English and French.</p>'''),
        ('controls', 'Controls', '''<table><thead><tr><th></th><th>Mac and Windows</th><th>iPhone, iPad and Android</th></tr></thead><tbody>
<tr><td>Walk</td><td>Cursor keys, or click the floor</td><td>Tap (or drag on) the floor</td></tr>
<tr><td>Go through a door</td><td>Walk into it, or click it</td><td>Tap it</td></tr>
<tr><td>Take an object</td><td><kbd>Space</kbd>, or click it</td><td>Tap it</td></tr>
<tr><td>Choose an object</td><td><kbd>1</kbd>–<kbd>4</kbd> or <kbd>Tab</kbd>, or click its slot</td><td>Tap its slot</td></tr>
<tr><td>Use / drop it</td><td><kbd>U</kbd> / <kbd>D</kbd>, or the buttons</td><td>Tap the chosen slot again, or the buttons</td></tr>
<tr><td>Pause / menu</td><td><kbd>P</kbd> / <kbd>Esc</kbd></td><td>Tap with three fingers / Back</td></tr>
<tr><td>Language / volume</td><td><kbd>L</kbd> / <kbd>V</kbd></td><td>Menu</td></tr>
<tr><td>Full screen</td><td><kbd>F11</kbd> or <kbd>Alt</kbd>+<kbd>Enter</kbd></td><td>Always</td></tr>
</tbody></table>
<p>Game controllers work too: the stick walks, A takes, X uses, B drops and the shoulder button changes object.</p>'''),
        ('castle', 'The castle', '''<ul>
<li><strong>Twenty rooms on four floors:</strong> the cellars, the ground floor, the lord's apartments and the eagle's eyrie at the top of the tower. Stairs are marked with an arrow; exits behind you are marked on the floor.</li>
<li><strong>Coloured doors</strong> (red, green, blue, yellow) are locked. Walk into one carrying the key of the same colour and it opens for good; the key stays in the lock.</li>
<li><strong>Dark rooms:</strong> without the lamp you can't see objects or traps. Carry the lamp and it lights the room around you.</li>
<li><strong>Trapdoors</strong> drop you to the floor below, and hurt. One pit in the catacombs has no bottom at all.</li>
<li><strong>Ghosts</strong> drift towards you and chill you. Carry the cross and they flee. <strong>Bats</strong> bite; <strong>guards</strong> in armour push you back.</li>
<li><strong>Energy</strong> ebbs away slowly. Eat bread to restore it. Potions heal you, except for one colour, which is poison; which one changes every game.</li>
<li>You can carry <strong>four objects</strong>. Treasure (coins, jewels, a goblet, a crown) goes straight into your score.</li>
<li>Bring the eagle back out through the <strong>gate</strong> in the gatehouse to win. The bonus grows with your remaining energy and the speed of your escape.</li>
</ul>'''),
        ('faq', 'Questions', '''<h3>A door won't open.</h3><p>Coloured doors need the key of the same colour, in your inventory. Plain archways and stairs are always open.</p>
<h3>I can't see anything.</h3><p>You're in a dark room. Find the lamp first; until then, feel your way to an exit, and beware of the floor.</p>
<h3>How do I change the language?</h3><p>Choose <em>Language</em> in the menu, or press <kbd>L</kbd> on a keyboard. The first time, the game follows your device's language.</p>
<h3>Where are my scores kept?</h3><p>Only on your device, in the game's own storage. Uninstalling the game deletes them.</p>
<h3>Can I play with a controller?</h3><p>Yes, on Mac, Windows, iPad, iPhone and Android, with any controller the system recognises.</p>'''),
        ('privacy', 'Privacy', '''<p><strong>Golden Eagle collects nothing.</strong> It has no accounts, advertising, analytics or tracking, and it never connects to the internet. It doesn't collect, store, share or sell any personal information, from anyone, including children.</p>
<p>The game keeps two small files in its private storage on your device: the Hall of Fame (the names you enter, with your scores) and your settings (language and volume). They never leave your device and are deleted when you uninstall the game.</p>
<p>If this policy changes, the new version will be posted here with a new date. Last updated: 5 October 2026.</p>'''),
        ('contact', 'Contact', f'<p>Found a bug, or have a question this page doesn\'t answer? Email <a href="mailto:{EMAIL}?subject=Golden%20Eagle%20support">{EMAIL}</a>.</p>'),
    ]

    nav = ''.join(f'<a href="#{sid}">{title}</a>' for sid, title, _ in sections)
    body = ''.join(f'<section id="{sid}"><h2>{title}</h2>{content}</section>' for sid, title, content in sections)

    GALLERY = gallery_html
    html = f'''<!doctype html>
<html lang="en-GB">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Golden Eagle Support</title>
<meta name="description" content="Help and support for Golden Eagle (L'Aigle d'Or), the high-resolution remake of the 1984 Oric castle adventure, for iPhone, iPad, Android, Mac and Windows: controls, how to play, questions, privacy and contact.">
<link rel="icon" href="{icon}">
<style>
:root {{ --bg: #F4F2EE; --pane: #FFFFFF; --chip: #E9E5DC; --text: #1E1B18; --muted: #6B655C;
  --accent: #9A6A08; --accent-2: #2C4AA8; --border: #DDD7CB; }}
@media (prefers-color-scheme: dark) {{
  :root:not([data-theme="light"]) {{ --bg: #0E0F1E; --pane: #171A30; --chip: #252946; --text: #EEECE6;
    --muted: #A9A8B8; --accent: #F0C04A; --accent-2: #8FB4FF; --border: #2E3355; }}
}}
:root[data-theme="dark"] {{ --bg: #0E0F1E; --pane: #171A30; --chip: #252946; --text: #EEECE6;
  --muted: #A9A8B8; --accent: #F0C04A; --accent-2: #8FB4FF; --border: #2E3355; }}
* {{ box-sizing: border-box; }}
html {{ scroll-behavior: smooth; }}
body {{ margin: 0; background: var(--bg); color: var(--text);
  font: 16px/1.6 -apple-system, BlinkMacSystemFont, "Segoe UI", system-ui, Roboto, "Helvetica Neue", Arial, sans-serif; }}
a {{ color: var(--accent); }}
.wrap {{ max-width: 1000px; margin: 0 auto; padding: 0 16px; }}
header {{ background: var(--pane); border-bottom: 1px solid var(--border); }}
header .wrap {{ padding-top: 32px; padding-bottom: 26px; }}
.top {{ display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap; }}
.brand {{ display: flex; align-items: center; gap: 16px; }}
.brand img {{ width: 72px; height: 72px; border-radius: 16px; }}
.eyebrow {{ color: var(--accent-2); font-weight: 600; margin: 0; }}
h1 {{ font-size: clamp(28px, 5vw, 40px); line-height: 1.15; margin: 0; }}
.lead {{ font-size: 18px; color: var(--muted); margin: 16px 0 0; max-width: 48em; }}
.hero {{ width: 100%; border-radius: 14px; margin-top: 22px; display: block; }}
nav.toc {{ display: flex; flex-wrap: wrap; gap: 8px; margin-top: 22px; }}
nav.toc a {{ text-decoration: none; color: var(--text); background: var(--chip); border-radius: 999px; padding: 6px 14px; font-size: 14px; }}
main section {{ background: var(--pane); border: 1px solid var(--border); border-radius: 14px; padding: 8px 24px 16px; margin: 20px 0; }}
h2 {{ font-size: 24px; margin: 16px 0 8px; }}
h3 {{ font-size: 17px; margin: 18px 0 2px; }}
table {{ width: 100%; border-collapse: collapse; font-size: 15px; }}
.table {{ overflow-x: auto; }}
th, td {{ text-align: left; padding: 8px 10px; border-bottom: 1px solid var(--border); vertical-align: top; }}
th {{ color: var(--muted); font-weight: 600; }}
kbd {{ font: 13px ui-monospace, SFMono-Regular, Menlo, monospace; background: var(--chip); border: 1px solid var(--border);
  border-radius: 5px; padding: 1px 5px; }}
.gallery {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; margin: 14px 0 6px; }}
.gallery img {{ width: 100%; border-radius: 10px; display: block; }}
footer {{ color: var(--muted); font-size: 14px; padding: 8px 0 40px; }}
</style>
</head>
<body>
<header><div class="wrap">
  <div class="top">
    <div class="brand"><img src="{icon}" alt="">
      <div><p class="eyebrow">{'Support'}</p><h1>{'Golden Eagle'}</h1></div></div>
  </div>
  <p class="lead">{"The 1984 Oric castle adventure, remade in high resolution for iPhone, iPad, Android, Mac and Windows. In English and French."}</p>
  {hero_html}
  <nav class="toc">{nav}</nav>
</div></header>
<main class="wrap">
{body.replace('<table>', '<div class="table"><table>').replace('</table>', '</table></div>').replace('</section><section id="controls">', GALLERY + '</section><section id="controls">', 1)}
</main>
<footer class="wrap">{"By PFJ, based on the 1984 Loriciels game <em>L'Aigle d'Or</em> by Louis-Marie Rocques. © 2026 Paul F. Johnson."}</footer>
</body>
</html>
'''
    with open(OUT, 'w') as f:
        f.write(html)
    dl = os.path.join(os.path.expanduser('~'), 'Downloads', 'GoldenEagle-Support.html')
    shutil.copy(OUT, dl)
    print(f'{os.path.relpath(OUT, ROOT)} ({len(html) // 1024} KB), copied to {dl}')


if __name__ == '__main__':
    main()
