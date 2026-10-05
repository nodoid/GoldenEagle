# Store listing: Golden Eagle / L'Aigle d'Or

Paste-ready sheets for each store and language are generated from this file into `store/<en|fr>/<store>/UPLOAD.txt` by `tools/make_upload_copy.py`.

Everything to paste into App Store Connect (iOS and Mac), Google Play Console and Microsoft Partner Center, in **English** and **French**. Every field is within its store's limit (checked by `tools/check_listing.py`). The screenshots, graphics and app previews are generated into `store/en/` and `store/fr/` by `tools/capture_all.sh`.

> **Before you submit:**
> - The game is a new remake *inspired by* the 1984 Oric game *L'Aigle d'Or* (Loriciels, by Louis-Marie Rocques). The castle, graphics, sound and code are all new; nothing was taken from the original. The French name and the credit do use the original's title and its author's name, though. Stores can reject apps that use another party's name or IP without permission, so make sure you have the rights (or permission) to use the title *L'Aigle d'Or* before you publish. If not, rename the French title in `Strings.cs` and in the platform name files.
> - Age rating: no blood or killing, but ghosts and a dark castle. Answer "Infrequent/Mild Horror/Fear Themes" (Apple 9+); the IARC questionnaire will probably give PEGI 7 / Everyone.

---

## Shared details

| Field | Value |
|---|---|
| App name | Golden Eagle (French: L'Aigle d'Or) |
| Bundle ID (iOS, macOS) | `uk.co.allthejohnsons.goldeneagle` |
| Package name (Android) | `uk.co.allthejohnsons.goldeneagle` |
| Android upload key | `~/keys/goldeneagle-upload.jks`, alias `goldeneagle`, password in Keychain ("GoldenEagle Android upload keystore"); SHA-256 `91:BD:CD:D1:1E:39:47:C7:16:87:02:0A:6A:86:6D:FC:79:D4:70:D7:88:5E:E7:F9:65:AC:6C:F2:4C:0E:E7:EA` |
| Windows package identity | `49556nodoid.GoldenEagle`, publisher `CN=A6EAEB04-6634-41C1-BDFE-695819ECE444` |
| Provisioning profiles | iOS `rel-eagle` (App Store), `devel-eagle`; macOS `red-eagle-macos` (App Store), `devel-eagle-macos` |
| Developer / seller | Paul F. Johnson |
| Category | Games, then Adventure (secondary: Puzzle) |
| Price | Free (or set your own) |
| Contains ads | No |
| In-app purchases | No |
| Languages | English, French |
| Support email | paul@all-the-johnsons.co.uk |
| Support URL | Host `docs/GoldenEagle-Support.html` (copy in `~/Downloads`) and paste its URL here |
| Privacy policy URL | The support page's `#privacy` section (the policy is also in `docs/PRIVACY.md`) |
| Age rating | Apple 9+ (infrequent/mild horror/fear themes); PEGI 7; ESRB Everyone |
| Supported devices | iPhone, iPad, Android phones and tablets, Macs with Apple silicon or Intel (macOS 12 or later), Windows 10/11 PCs (x64 and Arm) |
| Orientation | Landscape only |

---

## Apple App Store (iPhone and iPad)

### English

**Name** (30 max)
```
Golden Eagle
```

**Subtitle** (30 max)
```
A castle adventure in 3D rooms
```

**Promotional text** (170 max)
```
The classic 1984 Oric adventure, remade in high resolution. Explore a haunted castle, unlock its coloured doors and bring the Golden Eagle home. In English and French.
```

**Keywords** (100 max)
```
adventure,castle,retro,oric,eagle,ghosts,keys,dungeon,explore,quest,classic,french,1984,aigle,puzzle
```

**Description** (4000 max)
```
A treacherous lord has stolen the Golden Eagle, the magic statuette that protects the kingdom, and hidden it at the top of his castle. You enter alone, by the gatehouse. Find the eagle and bring it back out.

A REMAKE OF A CLASSIC
Golden Eagle is a new, high-resolution remake of L'Aigle d'Or, the 1984 Oric adventure by Louis-Marie Rocques. Each room is seen in perspective through its open front wall, and you walk your hero around it, from the gatehouse to the cellars, from the throne room to the tower.

EXPLORE THE CASTLE
- Twenty rooms on four floors: cellars, ground floor, the lord's apartments and the eagle's eyrie at the top of the tower
- Red, green, blue and yellow doors, each opened by its own key
- Dark catacombs where only the lamp shows the way, and the traps
- Trapdoors that drop you into the dungeons below
- Treasure to find: gold coins, jewels, a goblet and a crown

BEWARE
- Ghosts that drift after you, unless you hold up the cross
- Bats in the cellars and the tower
- Guards in armour who push you back
- Potions that heal you, or poison you: which colour is which changes every game
- Your strength ebbs away. Eat bread to keep going.

PLAY YOUR WAY
- Tap the floor to walk, a door to go through it, an object to pick it up
- Four inventory slots; use or drop the object you choose
- Play in English or French, and switch at any time
- Hall of Fame with name entry

HIGH RESOLUTION
Shaded stone walls, flickering torchlight, soft shadows and crisp artwork at the full resolution of your screen, with the spirit and colours of the original.

No ads, no tracking and no internet connection needed.

By PFJ, based on the 1984 Loriciels game by Louis-Marie Rocques.
```

**What's new** (4000 max)
```
First release.
```

### Français

**Nom** (30 max)
```
L'Aigle d'Or
```

**Sous-titre** (30 max)
```
L'aventure du château en 3D
```

**Texte promotionnel** (170 max)
```
Le grand classique Oric de 1984 revient en haute résolution. Explorez un château hanté, ouvrez ses portes de couleur et rapportez l'Aigle d'Or. En français et en anglais.
```

**Mots-clés** (100 max)
```
aventure,château,rétro,oric,aigle,fantômes,clés,donjon,exploration,quête,classique,loriciels
```

**Description** (4000 max)
```
Un seigneur félon a volé l'Aigle d'Or, la statuette magique qui protège le royaume, et l'a cachée tout en haut de son château. Vous entrez seul par le porche. Trouvez l'aigle et ressortez avec lui.

LE RETOUR D'UN CLASSIQUE
L'Aigle d'Or est un remake en haute résolution du célèbre jeu d'aventure Oric de 1984 de Louis-Marie Rocques. Chaque salle est vue en perspective, mur de façade ouvert, et vous y déplacez votre héros, du porche aux caves, de la salle du trône à la tour.

EXPLOREZ LE CHÂTEAU
- Vingt salles sur quatre niveaux : les caves, le rez-de-chaussée, les appartements du seigneur et l'aire de l'aigle au sommet de la tour
- Des portes rouge, verte, bleue et jaune, chacune ouverte par sa clé
- Des catacombes obscures où seule la lampe montre le chemin... et les pièges
- Des trappes qui vous précipitent dans les oubliettes
- Des trésors : pièces d'or, bijoux, calice et couronne

ATTENTION
- Les fantômes vous poursuivent, sauf si vous brandissez la croix
- Des chauves-souris dans les caves et la tour
- Des gardes en armure qui vous repoussent
- Des fioles qui soignent... ou empoisonnent : la couleur change à chaque partie
- Vos forces s'épuisent : mangez du pain pour tenir

JOUEZ À VOTRE FAÇON
- Touchez le sol pour marcher, une porte pour la franchir, un objet pour le prendre
- Quatre emplacements d'inventaire : utilisez ou posez l'objet choisi
- Jouez en français ou en anglais, et changez quand vous voulez
- Tableau d'honneur avec saisie du nom

HAUTE RÉSOLUTION
Murs de pierre ombrés, torches vacillantes, ombres douces et dessins nets à la pleine résolution de votre écran, dans l'esprit et les couleurs de l'original.

Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1984 de Louis-Marie Rocques.
```

**Nouveautés** (4000 max)
```
Première version.
```

**Screenshots:** `store/<en|fr>/app-store/iphone-6.9in-2868x1320/` (also `iphone-6.5in-2688x1242/`) and `ipad-13in-2752x2064/`, 10 each, landscape.
**App previews:** `store/<en|fr>/app-store/app-preview-iphone-1920x886.mp4` (iPhone 6.9") and `app-preview-ipad-1600x1200.mp4` (iPad 13"), 29.4 s, H.264 30 fps with stereo AAC. Copies are in `~/Movies/Golden Eagle/`.
**Icon:** `store/en/app-store/icon-1024.png`.

---

## Mac App Store

Use the same name, subtitle, promotional text and keywords as iOS, in each language.

### English

**Description** (4000 max)
```
A treacherous lord has stolen the Golden Eagle, the magic statuette that protects the kingdom, and hidden it at the top of his castle. You enter alone, by the gatehouse. Find the eagle and bring it back out.

A REMAKE OF A CLASSIC
Golden Eagle is a new, high-resolution remake of L'Aigle d'Or, the 1984 Oric adventure by Louis-Marie Rocques. Each room is seen in perspective through its open front wall, and you walk your hero around it, from the gatehouse to the cellars, from the throne room to the tower.

EXPLORE THE CASTLE
- Twenty rooms on four floors, from the cellars to the eagle's eyrie
- Red, green, blue and yellow doors, each opened by its own key
- Dark catacombs where only the lamp shows the way, and the traps
- Trapdoors, ghosts, bats and guards in armour
- Potions that heal you, or poison you: which colour is which changes every game

CONTROLS
Keyboard: cursor keys to walk, SPACE to take, 1 to 4 or TAB to choose an object, U to use it, D to drop it, P to pause, L to change language, F11 for full screen.
Mouse: click the floor to walk, a door to go through it, an object to pick it up.

HIGH RESOLUTION
Shaded stone walls, flickering torchlight, soft shadows and crisp artwork at the full resolution of your display, in a resizable window or full screen.

In English and French. No ads, no tracking and no internet connection needed.

By PFJ, based on the 1984 Loriciels game by Louis-Marie Rocques.
```

### Français

**Description** (4000 max)
```
Un seigneur félon a volé l'Aigle d'Or, la statuette magique qui protège le royaume, et l'a cachée tout en haut de son château. Vous entrez seul par le porche. Trouvez l'aigle et ressortez avec lui.

LE RETOUR D'UN CLASSIQUE
L'Aigle d'Or est un remake en haute résolution du célèbre jeu d'aventure Oric de 1984 de Louis-Marie Rocques. Chaque salle est vue en perspective, mur de façade ouvert, et vous y déplacez votre héros, du porche aux caves, de la salle du trône à la tour.

EXPLOREZ LE CHÂTEAU
- Vingt salles sur quatre niveaux, des caves à l'aire de l'aigle
- Des portes rouge, verte, bleue et jaune, chacune ouverte par sa clé
- Des catacombes obscures où seule la lampe montre le chemin... et les pièges
- Trappes, fantômes, chauves-souris et gardes en armure
- Des fioles qui soignent... ou empoisonnent : la couleur change à chaque partie

COMMANDES
Clavier : flèches pour marcher, ESPACE pour prendre, 1 à 4 ou TAB pour choisir un objet, U pour l'utiliser, D pour le poser, P pour la pause, L pour changer de langue, F11 pour le plein écran.
Souris : cliquez sur le sol pour marcher, sur une porte pour la franchir, sur un objet pour le prendre.

HAUTE RÉSOLUTION
Murs de pierre ombrés, torches vacillantes, ombres douces et dessins nets à la pleine résolution de votre écran, en fenêtre redimensionnable ou en plein écran.

En français et en anglais. Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1984 de Louis-Marie Rocques.
```

**Screenshots:** `store/<en|fr>/mac-app-store/screenshots-2880x1800/` (or `screenshots-1440x900/`), 10.
**App preview:** `store/<en|fr>/mac-app-store/app-preview-1920x1080.mp4`, 29.4 s. Copies are in `~/Movies/Golden Eagle/`.

---

## Google Play

### English

**App name** (30 max)
```
Golden Eagle
```

**Short description** (80 max)
```
Explore a haunted castle in 3D rooms and bring the Golden Eagle home.
```

**Full description** (4000 max)
```
A treacherous lord has stolen the Golden Eagle, the magic statuette that protects the kingdom, and hidden it at the top of his castle. You enter alone, by the gatehouse. Find the eagle and bring it back out.

Golden Eagle is a new, high-resolution remake of L'Aigle d'Or, the 1984 Oric adventure by Louis-Marie Rocques. Each room is seen in perspective through its open front wall, and you walk your hero around it.

EXPLORE THE CASTLE
• Twenty rooms on four floors, from the cellars to the eagle's eyrie
• Red, green, blue and yellow doors, each opened by its own key
• Dark catacombs where only the lamp shows the way
• Trapdoors, ghosts, bats and guards in armour
• Potions that heal, or poison: the colours change every game
• Treasure: gold coins, jewels, a goblet and a crown

PLAY
• Tap the floor to walk, a door to go through it, an object to take it
• Four inventory slots, with Use and Drop buttons
• English and French, switchable at any time
• Hall of Fame

No ads, no tracking and no internet connection needed.

By PFJ, based on the 1984 Loriciels game by Louis-Marie Rocques.
```

### Français

**Nom de l'application** (30 max)
```
L'Aigle d'Or
```

**Description courte** (80 max)
```
Explorez un château hanté en 3D et rapportez l'Aigle d'Or.
```

**Description complète** (4000 max)
```
Un seigneur félon a volé l'Aigle d'Or, la statuette magique qui protège le royaume, et l'a cachée tout en haut de son château. Vous entrez seul par le porche. Trouvez l'aigle et ressortez avec lui.

L'Aigle d'Or est un remake en haute résolution du célèbre jeu d'aventure Oric de 1984 de Louis-Marie Rocques. Chaque salle est vue en perspective, mur de façade ouvert, et vous y déplacez votre héros.

EXPLOREZ LE CHÂTEAU
• Vingt salles sur quatre niveaux, des caves à l'aire de l'aigle
• Des portes rouge, verte, bleue et jaune, chacune ouverte par sa clé
• Des catacombes obscures où seule la lampe montre le chemin
• Trappes, fantômes, chauves-souris et gardes en armure
• Des fioles qui soignent... ou empoisonnent : les couleurs changent à chaque partie
• Des trésors : pièces d'or, bijoux, calice et couronne

JOUEZ
• Touchez le sol pour marcher, une porte pour la franchir, un objet pour le prendre
• Quatre emplacements d'inventaire, avec les boutons Utiliser et Poser
• Français et anglais, à changer quand vous voulez
• Tableau d'honneur

Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1984 de Louis-Marie Rocques.
```

**Screenshots:** `store/<en|fr>/google-play/phone-screenshots-1920x1080/` and `tablet-screenshots-2560x1600/`, 8 each.
**Graphics:** `store/<en|fr>/google-play/icon-512.png`, `feature-graphic-1024x500.png`.
**Data safety:** no data collected or shared.

---

## Microsoft Store

### English

**Description** (10000 max)
```
A treacherous lord has stolen the Golden Eagle, the magic statuette that protects the kingdom, and hidden it at the top of his castle. You enter alone, by the gatehouse. Find the eagle and bring it back out.

Golden Eagle is a new, high-resolution remake of L'Aigle d'Or, the 1984 Oric adventure by Louis-Marie Rocques. Each room is seen in perspective through its open front wall, and you walk your hero around it: twenty rooms on four floors, coloured doors and their keys, dark catacombs, trapdoors, ghosts, bats and guards in armour.

Keyboard: cursor keys to walk, SPACE to take, 1 to 4 or TAB to choose an object, U to use it, D to drop it, P to pause, L to change language, F11 for full screen. Or click with the mouse. Game controllers work too.

In English and French. No ads, no tracking and no internet connection needed.

By PFJ, based on the 1984 Loriciels game by Louis-Marie Rocques.
```

**Product features** (20 max, 200 characters each)
```
Twenty rooms on four floors, seen in perspective as in the 1984 original
Coloured doors, each opened by its own key
Dark catacombs lit only by your lamp
Ghosts, bats, trapdoors and guards in armour
Potions that heal or poison, different every game
High-resolution graphics in a window or full screen
English and French
Keyboard, mouse or game controller
```

### Français

**Description** (10000 max)
```
Un seigneur félon a volé l'Aigle d'Or, la statuette magique qui protège le royaume, et l'a cachée tout en haut de son château. Vous entrez seul par le porche. Trouvez l'aigle et ressortez avec lui.

L'Aigle d'Or est un remake en haute résolution du célèbre jeu d'aventure Oric de 1984 de Louis-Marie Rocques. Chaque salle est vue en perspective, mur de façade ouvert, et vous y déplacez votre héros : vingt salles sur quatre niveaux, des portes de couleur et leurs clés, des catacombes obscures, des trappes, des fantômes, des chauves-souris et des gardes en armure.

Clavier : flèches pour marcher, ESPACE pour prendre, 1 à 4 ou TAB pour choisir un objet, U pour l'utiliser, D pour le poser, P pour la pause, L pour changer de langue, F11 pour le plein écran. Ou cliquez avec la souris. Les manettes fonctionnent aussi.

En français et en anglais. Sans publicité, sans pistage et sans connexion Internet.

Par PFJ, d'après le jeu Loriciels de 1984 de Louis-Marie Rocques.
```

**Fonctionnalités** (20 max, 200 characters each)
```
Vingt salles sur quatre niveaux, vues en perspective comme dans l'original de 1984
Des portes de couleur, chacune ouverte par sa clé
Des catacombes obscures éclairées par votre seule lampe
Fantômes, chauves-souris, trappes et gardes en armure
Des fioles qui soignent ou empoisonnent, différentes à chaque partie
Graphismes haute résolution, en fenêtre ou en plein écran
Français et anglais
Clavier, souris ou manette
```

**Screenshots:** `store/<en|fr>/microsoft-store/screenshots-3840x2160/` (also 1920x1080 and 1366x768), 10, captured by the Windows build.
**Art:** `box-art-2160x2160.png`, `poster-art-1440x2160.png`, `super-hero-art-1920x1080.png` / `3840x2160.png` (no text, as required), `store-logo-300x300.png`.
**Restricted capability:** `runFullTrust` is required because the game is a desktop (Win32) app packaged as MSIX. It reads and writes only its own settings and high scores.
