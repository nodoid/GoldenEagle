# Golden Eagle (L'Aigle d'Or)

A high-resolution MonoGame remake of **L'Aigle d'Or**, the 1984 Oric adventure by Louis-Marie
Rocques (Loriciels), for Android, iOS, macOS and Windows, in **English and French**.

A treacherous lord has hidden the Golden Eagle at the top of his castle. You enter by the gatehouse,
explore twenty rooms on four floors (each seen in perspective through its open front wall), open the
red, green, blue and yellow doors with their keys, light the catacombs with the lamp, keep the ghosts
at bay with the cross, and carry the eagle back out through the gate.

By PFJ, based on the 1984 Loriciels game. Released under the [DILLIGAF License](LICENSE).

## How it looks

- **Layout:** the Oric's 240 x 224 screen and 40-column text grid, so every screen keeps the
  original's proportions.
- **Rendering:** at the full resolution of the display (up to 8x on screen; 10x for store
  captures). Room surfaces are filled per device pixel row with smooth shading: stone courses,
  flagstones, arched doorways, torch glow, the lamp's pool of light in dark rooms, soft shadows.
- **Artwork:** every figure, object and piece of scenery is a vector illustration drawn by
  `tools/make_art.py` (supersampled, Lanczos-reduced) and embedded in `GoldenEagle.Core/Art`.
  The pixel maps in `SpriteData.cs` give each picture its size and proportions (and are the
  fallback if a picture is missing).
- **Text:** the Oric ROM font, smoothed with Scale2x, with added French accents.
- **Sound:** square waves and noise in the style of the Oric's AY-3-8912 (`Audio/Synth.cs`).

## Languages

All text is in `GoldenEagle.Core/Localization/Strings.cs` (French first, as in the original). The
first run follows the device language; **L** or the menu switches at any time, and the choice is
saved. The app name is localised too: "Golden Eagle" / "L'Aigle d'Or" (iOS and macOS
`InfoPlist.strings`, Android `values-fr`, Windows manifest).

## Projects

| Project | Target |
|---|---|
| `GoldenEagle.Core` | The whole game (net10.0): castle, rules, renderer, scenes, input, audio, capture mode |
| `GoldenEagle.Android` | Android 6+ |
| `GoldenEagle.iOS` | iOS 15+ (UIScene life cycle) |
| `GoldenEagle.DesktopGL` | macOS (also runs on Linux and Windows) |
| `GoldenEagle.WindowsDX` | Windows (DirectX), packaged as MSIX for the Microsoft Store |
| `GoldenEagle.Tests` | xUnit tests, including autopilot runs proving the castle can be completed |

Identifiers:
- Bundle ID (iOS and macOS) and Android package: `uk.co.allthejohnsons.goldeneagle`
- Windows identity: `49556nodoid.GoldenEagle`
- Profiles (read from `~/Downloads`, never committed): iOS `rel-eagle` / `devel-eagle`,
  macOS `redeaglemacos.provisionprofile` / `develeaglemacos.provisionprofile`

## Controls

| | Desktop | Phone / tablet |
|---|---|---|
| Walk | Cursor keys, or click the floor | Tap (or drag on) the floor |
| Go through a door | Walk into it, or click it | Tap it |
| Take | `SPACE`, or click the object | Tap the object |
| Choose an object | `1`-`4`, `TAB`, or click its slot | Tap its slot |
| Use / drop | `U` / `D`, or the buttons | Tap the selected slot again, or the buttons |
| Pause / menu | `P` / `Esc` | Three-finger tap / Back |
| Language / volume | `L` / `V` | Menu |

## Build and run

```sh
dotnet test GoldenEagle.Tests                    # unit tests
dotnet run --project GoldenEagle.DesktopGL       # play on the Mac
python3 tools/make_art.py                        # redraw the artwork
python3 art/make_icons.py                        # regenerate every platform icon
```

Release builds go to `release/` (git-ignored):

```sh
tools/build_desktop.sh       # macOS universal .app + App Store .pkg   (tools/build_desktop.sh dev: runs locally)
tools/build_release.sh       # Android .aab/.apk (needs ~/keys/goldeneagle-upload.jks) and the iOS .ipa
tools/build_windows.sh       # Windows x64 + arm64 .msix/.zip, built and tested in the Parallels "Windows 11" VM
```

## Store assets

The listing copy for every store, in English and French, is in [`docs/LISTING.md`](docs/LISTING.md)
(`python3 tools/check_listing.py` checks the limits); the privacy policy is
[`docs/PRIVACY.md`](docs/PRIVACY.md). Screenshots, graphics and app previews are generated into
`store/en` and `store/fr` (git-ignored):

```sh
tools/build_windows.sh capture   # Microsoft Store stills from the Windows build (both languages)
tools/capture_all.sh             # every other capture, the store images, and the app preview videos
```

Capture mode (`GoldenEagle --capture <dir> stills|videos [--mobile] [--lang fr] [--scale n]`)
plays the castle on autopilot and saves frames at high resolution, recording the sound effects so
the videos get a matching soundtrack. The videos are also copied to `~/Movies/Golden Eagle`.

## Licence

The Golden Eagle code, artwork, tools and documentation are released under the
**[DILLIGAF License](LICENSE)**. The licence can't cover what isn't ours: the original
*L'Aigle d'Or* game and its name, the Oric ROM font, and third-party packages such as MonoGame,
which keep their own licences.
