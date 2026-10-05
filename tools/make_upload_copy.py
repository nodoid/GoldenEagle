#!/usr/bin/env python3
"""Writes paste-ready upload sheets for every store and language from docs/LISTING.md.

    python3 tools/make_upload_copy.py

Creates store/<en|fr>/<store>/UPLOAD.txt: each field in the order the store's console asks for it,
with its character count, followed by the files to upload and the answers to the store's
questions. docs/LISTING.md stays the single source of the copy.
"""
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LISTING = os.path.join(ROOT, 'docs', 'LISTING.md')
STORE = os.path.join(ROOT, 'store')
SUPPORT = 'https://<your web site>/GoldenEagle-Support.html  (upload ~/Downloads/GoldenEagle-Support.html)'
PRIVACY = 'https://<your web site>/GoldenEagle-Support.html#privacy'
LANG_NAMES = {'en': ('English', 'English (U.K.)', 'en-GB', 'en-gb'), 'fr': ('French', 'French', 'fr-FR', 'fr-fr')}

FIELD = re.compile(r'\*\*([^*]+)\*\* \((\d+) max[^)]*\)\n```\n(.*?)\n```', re.S)


def sections(text):
    """{store heading: {'English'|'Français': [(label, limit, body)]}}"""
    stores = {}
    for block in re.split(r'\n## ', text)[1:]:
        title, _, rest = block.partition('\n')
        langs = {}
        for part in re.split(r'\n### ', '\n' + rest)[1:]:
            lang, _, body = part.partition('\n')
            langs[lang.strip()] = [(m.group(1), int(m.group(2)), m.group(3)) for m in FIELD.finditer(body)]
        stores[title.strip()] = langs
    return stores


def heading(title, ch='='):
    return f'{title}\n{ch * len(title)}\n'


def field(label, limit, body, per_line=False):
    count = f'longest line {max(map(len, body.splitlines()))}/{limit}' if per_line else f'{len(body)}/{limit} characters'
    return f'{heading(f"{label.upper()}  ({count})", "-")}{body}\n\n'


def files(lines):
    return heading('FILES TO UPLOAD', '-') + '\n'.join(f'- {l}' for l in lines) + '\n\n'


def answers(rows):
    width = max(len(k) for k, _ in rows)
    return heading('ANSWERS IN THE CONSOLE', '-') + '\n'.join(f'{k.ljust(width)}  {v}' for k, v in rows) + '\n\n'


def write(lang, store, text):
    path = os.path.join(STORE, lang, store, 'UPLOAD.txt')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w') as f:
        f.write(text)
    print(os.path.relpath(path, ROOT))


def main():
    s = sections(open(LISTING).read())
    ios = s['Apple App Store (iPhone and iPad)']
    mac = s['Mac App Store']
    play = s['Google Play']
    ms = s['Microsoft Store']
    labels_ios = ['Name', 'Subtitle', 'Promotional text', 'Keywords', 'Description', "What's new"]

    for lang, key in (('en', 'English'), ('fr', 'Français')):
        name, asc_lang, play_lang, ms_lang = LANG_NAMES[lang]
        d = f'store/{lang}'
        app_info = [
            ('Bundle ID', 'uk.co.allthejohnsons.goldeneagle'),
            ('SKU', 'goldeneagle'),
            ('Primary language', 'English (U.K.); add French as a localisation'),
            ('Category', 'Games > Adventure (secondary: Games > Puzzle)'),
            ('Age rating', 'Infrequent/Mild Horror/Fear Themes; everything else None (gives 9+)'),
            ('Price', 'Free (or your choice); no in-app purchases'),
            ('Support URL', SUPPORT),
            ('Privacy Policy URL', PRIVACY),
            ('Copyright', '2026 Paul F. Johnson'),
            ('App Privacy', 'Data Not Collected'),
            ('Encryption', 'None (ITSAppUsesNonExemptEncryption is already false in Info.plist)'),
            ('Sign-in required', 'No'),
            ('Review notes', 'Offline single-player game; no account needed. A remake of the 1984 Oric game L\'Aigle d\'Or.'),
        ]

        # ---------------- App Store (iOS)
        fields = ios[key]
        t = heading(f'GOLDEN EAGLE - APP STORE (IPHONE AND IPAD) - {name.upper()}')
        t += f'App Store Connect > Golden Eagle > iOS App > 1.0 > {asc_lang}\n\n'
        for label, (_, limit, body) in zip(labels_ios, fields):
            t += field(label, limit, body)
        t += files([
            f'iPhone 6.9" screenshots (10): {d}/app-store/iphone-6.9in-2868x1320/',
            f'iPhone 6.5" screenshots (10, if asked): {d}/app-store/iphone-6.5in-2688x1242/',
            f'iPad 13" screenshots (10): {d}/app-store/ipad-13in-2752x2064/',
            f'iPhone app preview: {d}/app-store/app-preview-iphone-1920x886.mp4 (poster frame: about 0:12)',
            f'iPad app preview: {d}/app-store/app-preview-ipad-1600x1200.mp4',
            'App icon: comes from the build (store/en/app-store/icon-1024.png is the same picture)',
            'Build: release/ios/GoldenEagle.ipa (upload with Transporter; version 1.0.0, build 1)',
        ])
        if lang == 'en':
            t += answers(app_info)
        else:
            t += 'App information, pricing and privacy answers: see store/en/app-store/UPLOAD.txt (they are set once, for every language).\n'
        write(lang, 'app-store', t)

        # ---------------- Mac App Store
        t = heading(f'GOLDEN EAGLE - MAC APP STORE - {name.upper()}')
        t += f'App Store Connect > Golden Eagle > macOS App > 1.0 > {asc_lang}\n'
        t += 'Name, subtitle, promotional text and keywords are the same as on iOS.\n\n'
        for label, (_, limit, body) in zip(labels_ios, fields):
            if label in ('Description', "What's new"):
                continue
            t += field(label, limit, body)
        _, limit, body = mac[key][0]
        t += field('Description', limit, body)
        t += field("What's new", fields[5][1], fields[5][2])
        t += files([
            f'Screenshots (10): {d}/mac-app-store/screenshots-2880x1800/ (or screenshots-1440x900/)',
            f'App preview: {d}/mac-app-store/app-preview-1920x1080.mp4',
            'Build: release/macos/GoldenEagle.pkg (upload with Transporter; version 1.0.0, build 1)',
        ])
        if lang == 'en':
            t += answers([r for r in app_info if r[0] != 'Age rating'] +
                         [('Age rating', 'Same answers as iOS (9+)'), ('Minimum macOS', '12.0; universal (Apple silicon and Intel)')])
        else:
            t += 'App information, pricing and privacy answers: see store/en/mac-app-store/UPLOAD.txt.\n'
        write(lang, 'mac-app-store', t)

        # ---------------- Google Play
        t = heading(f'GOLDEN EAGLE - GOOGLE PLAY - {name.upper()}')
        t += f'Play Console > Golden Eagle > Grow > Store presence > Main store listing > {play_lang}\n\n'
        for label, (_, limit, body) in zip(['App name', 'Short description', 'Full description'], play[key]):
            t += field(label, limit, body)
        t += files([
            f'App icon: {d}/google-play/icon-512.png',
            f'Feature graphic: {d}/google-play/feature-graphic-1024x500.png',
            f'Phone screenshots (8): {d}/google-play/phone-screenshots-1920x1080/',
            f'10-inch tablet screenshots (8): {d}/google-play/tablet-screenshots-2560x1600/ (also fine for 7-inch)',
            'App bundle: release/android/uk.co.allthejohnsons.goldeneagle-Signed.aab (version 1.0.0, code 1)',
        ])
        if lang == 'en':
            t += answers([
                ('Package name', 'uk.co.allthejohnsons.goldeneagle'),
                ('App or game', 'Game; category Adventure'),
                ('Free or paid', 'Free'),
                ('Default language', 'English (United Kingdom) en-GB; add French (France) fr-FR'),
                ('App signing', 'Let Google manage the app signing key; this upload key: ~/keys/goldeneagle-upload.jks'),
                ('Privacy policy', PRIVACY),
                ('App access', 'All functionality available without special access'),
                ('Ads', 'No ads'),
                ('Content rating (IARC)', 'Game; no violence except fantasy (ghosts, bats); no blood; mild horror/fear: yes'),
                ('Target audience', '13 and over (or include younger ages if you prefer; nothing is collected)'),
                ('Data safety', 'No data collected; no data shared'),
                ('Government / financial / health', 'No'),
                ('Contact email', 'paul@all-the-johnsons.co.uk'),
                ('Website', SUPPORT),
            ])
        else:
            t += 'App content answers (privacy, ads, rating, data safety): see store/en/google-play/UPLOAD.txt.\n'
        write(lang, 'google-play', t)

        # ---------------- Microsoft Store
        t = heading(f'GOLDEN EAGLE - MICROSOFT STORE - {name.upper()}')
        t += f'Partner Center > Golden Eagle > Submission > Store listings > {ms_lang}\n\n'
        _, limit, body = ms[key][0]
        t += field('Description', limit, body)
        label, limit, body = ms[key][1]
        t += field('Product features (one per box)', 200, body, per_line=True)
        t += field('Short title', 50, 'Golden Eagle' if lang == 'en' else "L'Aigle d'Or")
        t += files([
            f'Screenshots (10): {d}/microsoft-store/screenshots-3840x2160/ (or 1920x1080 / 1366x768)',
            f'Box art 1:1: {d}/microsoft-store/box-art-2160x2160.png',
            f'Poster art 2:3: {d}/microsoft-store/poster-art-1440x2160.png',
            f'Super hero art 16:9 (no text): {d}/microsoft-store/super-hero-art-3840x2160.png',
            f'Store logo: {d}/microsoft-store/store-logo-300x300.png',
            'Packages: release/windows/GoldenEagle-1.0.0-x64.msix and GoldenEagle-1.0.0-arm64.msix',
        ])
        if lang == 'en':
            t += answers([
                ('Package identity', '49556nodoid.GoldenEagle'),
                ('Category', 'Games > Adventure'),
                ('Pricing', 'Free'),
                ('Age ratings', 'IARC questionnaire: same answers as Google Play'),
                ('Privacy policy URL', PRIVACY),
                ('Support contact', 'paul@all-the-johnsons.co.uk'),
                ('Website', SUPPORT),
                ('Input', 'Keyboard, mouse and game controller'),
                ('runFullTrust', 'Desktop (Win32) game packaged as MSIX; it reads and writes only its own settings and scores.'),
                ('Certification notes', 'Offline single-player game, no account needed. Arrow keys walk, Space takes, U uses, Esc for the menu.'),
            ])
        else:
            t += 'Properties, pricing and age rating answers: see store/en/microsoft-store/UPLOAD.txt.\n'
        write(lang, 'microsoft-store', t)


if __name__ == '__main__':
    main()
