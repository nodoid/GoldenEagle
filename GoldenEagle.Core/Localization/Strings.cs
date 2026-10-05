using System;

namespace GoldenEagle.Core.Localization;

public enum Language
{
    English,
    French,
}

/// <summary>A piece of text in both languages. <see cref="ToString"/> gives the current one.</summary>
public readonly record struct Loc(string Fr, string En)
{
    public string Get(Language language) => language == Language.French ? Fr : En;

    public override string ToString() => Get(Strings.Current);
}

/// <summary>
/// Every piece of text the game shows, in the original French and in English.
/// Accented capitals and lower case use the extra glyphs in <see cref="Oric.OricFont"/>.
/// </summary>
public static class Strings
{
    public static Language Current { get; set; } = Language.English;

    /// <summary>Picks the language for a first run from the device's two-letter language code.</summary>
    public static Language FromIso(string iso) =>
        string.Equals(iso, "fr", StringComparison.OrdinalIgnoreCase) ? Language.French : Language.English;

    public static string IsoCode(Language language) => language == Language.French ? "fr" : "en";

    private static string L(string fr, string en) => Current == Language.French ? fr : en;

    // ---------------- title and credits ----------------
    public static string Title => L("L'AIGLE D'OR", "THE GOLDEN EAGLE");
    public static string Subtitle => L("THE GOLDEN EAGLE", "L'AIGLE D'OR");
    public static string Credit => L("PAR PFJ", "BY PFJ");
    public static string BasedOn => L("D'APRÈS LE JEU LORICIELS DE 1984", "BASED ON THE 1984 LORICIELS GAME");
    public static string Author => L("DE LOUIS-MARIE ROCQUES", "BY LOUIS-MARIE ROCQUES");
    public static string PressToStart(bool mobile) =>
        mobile ? L("TOUCHEZ L'ÉCRAN", "TAP THE SCREEN") : L("APPUYEZ SUR ESPACE", "PRESS SPACE");

    // ---------------- menu ----------------
    public static string Play => L("JOUER", "PLAY");
    public static string Continue => L("REPRENDRE LA PARTIE", "CONTINUE GAME");
    public static string Instructions => L("INSTRUCTIONS", "INSTRUCTIONS");
    public static string HallOfFame => L("TABLEAU D'HONNEUR", "HALL OF FAME");
    public static string LanguageItem => L("LANGUE : FRANÇAIS", "LANGUAGE: ENGLISH");
    public static string Volume => L("VOLUME", "VOLUME");
    public static string Quit => L("QUITTER", "QUIT");
    public static string VolumeName(int level) => level switch
    {
        0 => L("COUPÉ", "OFF"),
        1 => L("FAIBLE", "LOW"),
        2 => L("MOYEN", "MEDIUM"),
        _ => L("FORT", "HIGH"),
    };

    // ---------------- HUD ----------------
    public static string Energy => L("ÉNERGIE", "ENERGY");
    public static string Score => L("SCORE", "SCORE");
    public static string Use => L("UTILISER", "USE");
    public static string Drop => L("POSER", "DROP");
    public static string Paused => L("PAUSE", "PAUSED");
    public static string Resume(bool mobile) => mobile ? L("TOUCHEZ", "TAP") : L("ESPACE", "SPACE");
    public static string Empty => L("VIDE", "EMPTY");

    // ---------------- messages ----------------
    public static string YouTake(string item) => L("VOUS PRENEZ : ", "YOU TAKE: ") + item;
    public static string YouDrop(string item) => L("VOUS POSEZ : ", "YOU DROP: ") + item;
    public static string HandsFull => L("VOS MAINS SONT PLEINES !", "YOUR HANDS ARE FULL!");
    public static string NothingHere => L("IL N'Y A RIEN ICI.", "THERE IS NOTHING HERE.");
    public static string NothingToUse => L("RIEN À UTILISER.", "NOTHING TO USE.");
    public static string DoorLocked(string key) => L("FERMÉ À CLÉ : IL FAUT ", "LOCKED: YOU NEED ") + key;
    public static string DoorUnlocked => L("LA PORTE S'OUVRE !", "THE DOOR OPENS!");
    public static string ItIsDark => L("IL FAIT NOIR COMME DANS UN FOUR...", "IT IS PITCH DARK...");
    public static string YouFall => L("UNE TRAPPE ! VOUS TOMBEZ...", "A TRAPDOOR! YOU FALL...");
    public static string GhostTouch => L("UN FANTÔME VOUS GLACE LE SANG !", "A GHOST CHILLS YOUR BLOOD!");
    public static string BatBite => L("UNE CHAUVE-SOURIS VOUS MORD !", "A BAT BITES YOU!");
    public static string KnightHit => L("LE GARDE VOUS REPOUSSE !", "THE GUARD PUSHES YOU BACK!");
    public static string Healed => L("VOUS VOUS SENTEZ MIEUX !", "YOU FEEL MUCH BETTER!");
    public static string Poisoned => L("POUAH ! C'ÉTAIT DU POISON !", "UGH! IT WAS POISON!");
    public static string Eaten => L("MIAM ! DES FORCES NOUVELLES.", "YUM! YOUR STRENGTH RETURNS.");
    public static string LampLit => L("LA LAMPE ÉCLAIRE LES TÉNÈBRES.", "THE LAMP LIGHTS THE DARKNESS.");
    public static string CrossRaised => L("LES FANTÔMES CRAIGNENT LA CROIX.", "GHOSTS FEAR THE CROSS.");
    public static string KeyHint => L("UNE CLÉ OUVRE SA PORTE.", "A KEY OPENS ITS OWN DOOR.");
    public static string EagleHint => L("RAPPORTEZ L'AIGLE AU PORCHE !", "TAKE THE EAGLE TO THE GATEHOUSE!");
    public static string Treasure(string item, int points) => item + " ! +" + points;
    public static string NeedEagle => L("PAS SANS L'AIGLE D'OR !", "NOT WITHOUT THE GOLDEN EAGLE!");
    public static string Tired => L("VOUS VOUS SENTEZ FAIBLE...", "YOU FEEL WEAK...");

    // ---------------- end of game ----------------
    public static string GameOver => L("PARTIE TERMINÉE", "GAME OVER");
    public static string Victory => L("VICTOIRE !", "VICTORY!");
    public static string VictoryText1 => L("VOUS AVEZ RAPPORTÉ", "YOU HAVE BROUGHT BACK");
    public static string VictoryText2 => L("L'AIGLE D'OR !", "THE GOLDEN EAGLE!");
    public static string DeathEnergy => L("VOUS ÊTES MORT D'ÉPUISEMENT.", "YOU DIED OF EXHAUSTION.");
    public static string DeathAbyss => L("VOUS ÊTES TOMBÉ DANS L'ABÎME.", "YOU FELL INTO THE ABYSS.");
    public static string RoomsExplored(int n, int total) => L($"SALLES EXPLORÉES : {n}/{total}", $"ROOMS EXPLORED: {n}/{total}");
    public static string TimeTaken(int seconds) =>
        L("TEMPS : ", "TIME: ") + $"{seconds / 60}:{seconds % 60:D2}";
    public static string Bonus => L("BONUS", "BONUS");
    public static string YourScore => L("VOTRE SCORE", "YOUR SCORE");
    public static string EnterName => L("ENTREZ VOTRE NOM", "ENTER YOUR NAME");
    public static string ChangeLetter(bool mobile) =>
        mobile ? L("TOUCHEZ < > POUR CHANGER", "TAP < > TO CHANGE LETTER") : L("FLÈCHES OU CLAVIER", "CURSOR KEYS OR TYPE");
    public static string AcceptLetter(bool mobile) =>
        mobile ? L("TOUCHEZ LA LETTRE POUR VALIDER", "TAP THE LETTER TO ACCEPT") : L("ESPACE POUR VALIDER", "SPACE TO ACCEPT");
    public static string EndMarkHint => L("< TERMINE LE NOM", "< ENDS THE NAME");
    public static string NameHeader => L("NOM", "NAME");
    public static string PlayOrMenu(bool mobile) =>
        mobile ? L("TOUCHEZ POUR JOUER", "TAP TO PLAY") : L("ESPACE : JOUER   ÉCHAP : MENU", "SPACE: PLAY   ESC: MENU");

    // ---------------- instructions ----------------
    public static string Page(int page, int pages) => L($"PAGE {page}/{pages}", $"PAGE {page}/{pages}");
    public static string NextPage(bool mobile) =>
        mobile ? L("TOUCHEZ POUR CONTINUER", "TAP TO CONTINUE") : L("ESPACE POUR CONTINUER", "SPACE TO CONTINUE");

    public static string[] Story => Current == Language.French
        ?
        [
            "IL ÉTAIT UNE FOIS UN ROYAUME PROTÉGÉ",
            "PAR UNE STATUETTE MAGIQUE :",
            "L'AIGLE D'OR.",
            "",
            "UN SEIGNEUR FÉLON L'A VOLÉE ET CACHÉE",
            "TOUT EN HAUT DE SON CHÂTEAU, GARDÉ PAR",
            "DES SPECTRES, DES CHAUVES-SOURIS ET",
            "DES GARDES EN ARMURE.",
            "",
            "VOUS ENTREZ SEUL PAR LE PORCHE.",
            "TROUVEZ L'AIGLE D'OR, PUIS RESSORTEZ",
            "PAR LE PORCHE POUR SAUVER LE ROYAUME.",
        ]
        :
        [
            "ONCE UPON A TIME A KINGDOM WAS GUARDED",
            "BY A MAGIC STATUETTE:",
            "THE GOLDEN EAGLE.",
            "",
            "A TREACHEROUS LORD STOLE IT AND HID IT",
            "AT THE VERY TOP OF HIS CASTLE, GUARDED",
            "BY GHOSTS, BATS AND GUARDS IN ARMOUR.",
            "",
            "YOU ENTER ALONE BY THE GATEHOUSE.",
            "FIND THE GOLDEN EAGLE, THEN LEAVE BY",
            "THE GATEHOUSE TO SAVE THE KINGDOM.",
        ];

    public static string[] Rules => Current == Language.French
        ?
        [
            "LES PORTES DE COULEUR SONT FERMÉES :",
            "LA CLÉ DE MÊME COULEUR LES OUVRE.",
            "",
            "SANS LAMPE, LES CAVES SONT NOIRES",
            "ET LEURS PIÈGES INVISIBLES.",
            "",
            "LA CROIX FAIT FUIR LES FANTÔMES.",
            "",
            "LE PAIN REDONNE DES FORCES. LES FIOLES",
            "SOIGNENT... OU EMPOISONNENT !",
            "",
            "ATTENTION AUX TRAPPES DANS LE SOL.",
            "VOUS NE PORTEZ QUE QUATRE OBJETS.",
        ]
        :
        [
            "COLOURED DOORS ARE LOCKED: THE KEY",
            "OF THE SAME COLOUR OPENS THEM.",
            "",
            "WITHOUT A LAMP THE CELLARS ARE DARK",
            "AND THEIR TRAPS ARE INVISIBLE.",
            "",
            "THE CROSS DRIVES GHOSTS AWAY.",
            "",
            "BREAD RESTORES YOUR STRENGTH. POTIONS",
            "HEAL YOU... OR POISON YOU!",
            "",
            "BEWARE OF TRAPDOORS IN THE FLOOR.",
            "YOU CAN ONLY CARRY FOUR THINGS.",
        ];

    public static (string key, string action)[] Controls(bool mobile) => mobile
        ? Current == Language.French
            ?
            [
                ("TOUCHER LE SOL", "MARCHER"),
                ("TOUCHER UNE PORTE", "Y ALLER"),
                ("TOUCHER UN OBJET", "LE PRENDRE"),
                ("TOUCHER UN OBJET", ""),
                ("  DE L'INVENTAIRE", "LE CHOISIR"),
                ("UTILISER / POSER", "SUR L'OBJET CHOISI"),
                ("TROIS DOIGTS", "PAUSE"),
                ("RETOUR", "MENU"),
            ]
            :
            [
                ("TAP THE FLOOR", "WALK THERE"),
                ("TAP A DOOR", "GO THROUGH IT"),
                ("TAP AN OBJECT", "PICK IT UP"),
                ("TAP AN INVENTORY", ""),
                ("  OBJECT", "SELECT IT"),
                ("USE / DROP", "ON THE SELECTION"),
                ("THREE FINGER TAP", "PAUSE"),
                ("BACK", "MENU"),
            ]
        : Current == Language.French
            ?
            [
                ("FLÈCHES", "MARCHER"),
                ("ESPACE", "PRENDRE"),
                ("1 2 3 4 / TAB", "CHOISIR UN OBJET"),
                ("U / ENTRÉE", "UTILISER"),
                ("D", "POSER"),
                ("SOURIS", "CLIQUER POUR ALLER"),
                ("P", "PAUSE"),
                ("L", "LANGUE"),
                ("V", "VOLUME"),
                ("ÉCHAP", "MENU"),
                ("F11", "PLEIN ÉCRAN"),
            ]
            :
            [
                ("CURSOR KEYS", "WALK"),
                ("SPACE", "TAKE"),
                ("1 2 3 4 / TAB", "SELECT AN OBJECT"),
                ("U / ENTER", "USE"),
                ("D", "DROP"),
                ("MOUSE", "CLICK TO GO THERE"),
                ("P", "PAUSE"),
                ("L", "LANGUAGE"),
                ("V", "VOLUME"),
                ("ESC", "MENU"),
                ("F11", "FULL SCREEN"),
            ];

    public static string StoryHeader => L("L'HISTOIRE", "THE STORY");
    public static string RulesHeader => L("LES RÈGLES", "THE RULES");
    public static string ControlsHeader => L("LES COMMANDES", "THE CONTROLS");
}
