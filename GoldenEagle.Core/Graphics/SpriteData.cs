namespace GoldenEagle.Core.Graphics;

/// <summary>
/// The game's pixel art, drawn for the Oric's eight colours. One character per pixel:
/// K black, R red, G green, Y yellow, B blue, M magenta, C cyan, W white; '.' is transparent.
/// </summary>
public static class SpriteData
{
    private static System.Collections.Generic.Dictionary<string[], string> _names;

    /// <summary>The field name of a map declared here, or null.</summary>
    public static string NameOf(string[] rows)
    {
        if (_names == null)
        {
            var names = new System.Collections.Generic.Dictionary<string[], string>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
            foreach (var f in typeof(SpriteData).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (f.GetValue(null) is string[] map)
                    names[map] = f.Name;
            _names = names;
        }
        return _names.TryGetValue(rows, out var name) ? name : null;
    }

    // ---------------------------------------------------------------- the hero (12 x 24)

    public static readonly string[] HeroFront0 =
    [
        "....RRRR....",
        "...RRRRRR...",
        "..RRRRRRRR..",
        "...WWWWWW...",
        "...WKWWKW...",
        "...WWWWWW...",
        "....WRRW....",
        ".....WW.....",
        "...GGGGGG...",
        "..GGGGGGGG..",
        ".GGGGGGGGGG.",
        ".GGGGGGGGGG.",
        ".GGYYYYYYGG.",
        ".WGGGGGGGGW.",
        "..GGGGGGGG..",
        "..GGGGGGGG..",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "..RRR..RRR..",
        "..RRR..RRR..",
    ];

    public static readonly string[] HeroFront1 =
    [
        "....RRRR....",
        "...RRRRRR...",
        "..RRRRRRRR..",
        "...WWWWWW...",
        "...WKWWKW...",
        "...WWWWWW...",
        "....WRRW....",
        ".....WW.....",
        "...GGGGGG...",
        "..GGGGGGGG..",
        ".GGGGGGGGGG.",
        ".GGGGGGGGGGW",
        "WGGYYYYYYGG.",
        "..GGGGGGGG..",
        "..GGGGGGGG..",
        "..GGGGGGGG..",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "..RRR..BB...",
        "..RRR..BB...",
        ".......RRR..",
        ".......RRR..",
    ];

    public static readonly string[] HeroBack0 =
    [
        "....RRRR....",
        "...RRRRRR...",
        "..RRRRRRRR..",
        "...YYYYYY...",
        "...YYYYYY...",
        "...YYYYYY...",
        "....YYYY....",
        ".....WW.....",
        "...GGGGGG...",
        "..GGGGGGGG..",
        ".GGGGGGGGGG.",
        ".GGGGGGGGGG.",
        ".GGYYYYYYGG.",
        ".WGGGGGGGGW.",
        "..GGGGGGGG..",
        "..GGGGGGGG..",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "..RRR..RRR..",
        "..RRR..RRR..",
    ];

    public static readonly string[] HeroBack1 =
    [
        "....RRRR....",
        "...RRRRRR...",
        "..RRRRRRRR..",
        "...YYYYYY...",
        "...YYYYYY...",
        "...YYYYYY...",
        "....YYYY....",
        ".....WW.....",
        "...GGGGGG...",
        "..GGGGGGGG..",
        ".GGGGGGGGGG.",
        "WGGGGGGGGGG.",
        ".GGYYYYYYGGW",
        "..GGGGGGGG..",
        "..GGGGGGGG..",
        "..GGGGGGGG..",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..BB...",
        "...BB..RRR..",
        "...BB..RRR..",
        "..RRR.......",
        "..RRR.......",
    ];

    /// <summary>Facing right; mirrored for left.</summary>
    public static readonly string[] HeroSide0 =
    [
        "....RRRR....",
        "...RRRRRR...",
        "...RRRRRRRR.",
        "....WWWWW...",
        "....WWWKW...",
        "....WWWWWW..",
        "....WWWRW...",
        ".....WW.....",
        "....GGGG....",
        "...GGGGGG...",
        "...GGWGGG...",
        "...GGWGGG...",
        "...YYWYYY...",
        "...GGWGGG...",
        "....GWGG....",
        "....GGGG....",
        "....BBBB....",
        "....BB.B....",
        "....BB.B....",
        "....BB.B....",
        "....BB.B....",
        "....BB.B....",
        "....RRRRR...",
        "....RRRRR...",
    ];

    public static readonly string[] HeroSide1 =
    [
        "....RRRR....",
        "...RRRRRR...",
        "...RRRRRRRR.",
        "....WWWWW...",
        "....WWWKW...",
        "....WWWWWW..",
        "....WWWRW...",
        ".....WW.....",
        "....GGGG....",
        "...GGGGGG...",
        "..WGGGGGGW..",
        ".W.GGGGGG.W.",
        "...YYYYYY...",
        "...GGGGGG...",
        "...GGGGGG...",
        "....GGGG....",
        "....BBBB....",
        "...BB..BB...",
        "...BB...BB..",
        "..BB....BB..",
        "..BB.....BB.",
        ".BB......BB.",
        "RRR......RRR",
        "RRR.......RR",
    ];

    // ---------------------------------------------------------------- monsters

    public static readonly string[] Ghost0 =
    [
        "....WWWW....",
        "..WWWWWWWW..",
        ".WWWWWWWWWW.",
        ".WWKKWWKKWW.",
        ".WWKKWWKKWW.",
        "WWWWWWWWWWWW",
        "WWWWWKKWWWWW",
        "WWWWKKKKWWWW",
        "WWWWWKKWWWWW",
        "WWWWWWWWWWWW",
        "WWWWWWWWWWWW",
        "WWWWWWWWWWWW",
        "WW.WWW.WWW.W",
        "W...W...W...",
    ];

    public static readonly string[] Ghost1 =
    [
        "....WWWW....",
        "..WWWWWWWW..",
        ".WWWWWWWWWW.",
        ".WWKKWWKKWW.",
        ".WWKKWWKKWW.",
        "WWWWWWWWWWWW",
        "WWWWWKKWWWWW",
        "WWWWKKKKWWWW",
        "WWWWWKKWWWWW",
        "WWWWWWWWWWWW",
        "WWWWWWWWWWWW",
        "WWWWWWWWWWWW",
        "W.WWW.WWW.WW",
        "..W...W...WW",
    ];

    public static readonly string[] Bat0 =
    [
        "M............M",
        "MM..........MM",
        "MMM..M..M..MMM",
        ".MMMMMMMMMMMM.",
        "..MMMMRRMMMM..",
        "...M..MM..M...",
    ];

    public static readonly string[] Bat1 =
    [
        "......M..M....",
        "......MMMM....",
        "...MMMMRRMMM..",
        ".MMMMMMMMMMMMM",
        "MMM...MM...MMM",
        "M............M",
    ];

    public static readonly string[] Knight0 =
    [
        ".....RR.....",
        "....RRR.....",
        "....CCCC....",
        "...CCCCCC...",
        "...CKKKKC...",
        "...CCCCCC...",
        "....CCCC....",
        "..CCCCCCCC..",
        ".CCCWCCWCCC.",
        ".CCCCCCCCCCW",
        ".CCCCCCCCCCW",
        ".CCWWWWWWCCW",
        ".CCCCCCCCCCW",
        "..CCCCCCCC.W",
        "..CCCCCCCC.W",
        "...CC..CC..W",
        "...CC..CC..W",
        "...CC..CC..W",
        "...CC..CC..W",
        "...CC..CC..W",
        "...CC..CC..W",
        "...CC..CC..W",
        "..CCC..CCC.W",
        "..CCC..CCC.W",
    ];

    public static readonly string[] Knight1 =
    [
        ".....RR.....",
        "....RRR.....",
        "....CCCC....",
        "...CCCCCC...",
        "...CKKKKC...",
        "...CCCCCC...",
        "....CCCC....",
        "..CCCCCCCC..",
        ".CCCWCCWCCC.",
        ".CCCCCCCCCCW",
        ".CCCCCCCCCCW",
        ".CCWWWWWWCCW",
        ".CCCCCCCCCCW",
        "..CCCCCCCC.W",
        "..CCCCCCCC.W",
        "...CC..CC..W",
        "...CC..CC..W",
        "..CC...CC..W",
        "..CC...CC..W",
        "..CC....CC.W",
        ".CC.....CC.W",
        ".CC.....CC.W",
        "CCC.....CCCW",
        "CCC......CCW",
    ];

    // ---------------------------------------------------------------- objects

    /// <summary>Drawn tinted in the key's colour.</summary>
    public static readonly string[] Key =
    [
        ".WWW.......",
        "W...W......",
        "W...WWWWWWW",
        "W...W..W.W.",
        ".WWW...W.W.",
    ];

    /// <summary>The flask is white glass; the liquid (R) is recoloured per potion.</summary>
    public static readonly string[] Potion =
    [
        "..YY..",
        "..WW..",
        "..WW..",
        ".WRRW.",
        "WRRRRW",
        "WRRRRW",
        "WRRRRW",
        ".WWWW.",
    ];

    public static readonly string[] Lamp =
    [
        "...R...",
        "..RYR..",
        "...Y...",
        ".WWWWW.",
        "W.YYY.W",
        "W.YYY.W",
        ".WWWWW.",
        "..WWW..",
    ];

    public static readonly string[] Cross =
    [
        "..Y..",
        ".YYY.",
        "YYYYY",
        ".YYY.",
        "..Y..",
        "..Y..",
        "..Y..",
        ".YYY.",
    ];

    public static readonly string[] Bread =
    [
        "..YYYY...",
        ".YRYRYRY.",
        "YYYYYYYYY",
        "YYYYYYYYY",
        ".YYYYYYY.",
    ];

    public static readonly string[] Eagle =
    [
        "Y..........Y",
        "YY...YY...YY",
        "YYY.YYRY.YYY",
        "YYYYYYYYYYYY",
        ".YYYYRYYYYY.",
        "..YYYYYYYY..",
        "....YYYY....",
        "...YY..YY...",
        "..RR....RR..",
        "..WWWWWWWW..",
        "...WWWWWW...",
        "...WWWWWW...",
        "..WWWWWWWW..",
    ];

    public static readonly string[] Coins =
    [
        "..YYY....",
        ".YYYYY.YY",
        "YYYYYYYYY",
        "YYYYYYYYY",
        ".YYYYYYY.",
    ];

    public static readonly string[] Goblet =
    [
        "YYYYYYY",
        "YRYRYRY",
        "YYYYYYY",
        ".YYYYY.",
        "..YYY..",
        "...Y...",
        "...Y...",
        ".YYYYY.",
    ];

    public static readonly string[] Crown =
    [
        "Y..Y..Y..Y",
        "YY.YY.YY.Y",
        "YYYYYYYYYY",
        "YRYYCYYRYY",
        "YYYYYYYYYY",
    ];

    /// <summary>Gems; the R pixels are recoloured.</summary>
    public static readonly string[] Jewels =
    [
        "..W....W..",
        ".WRW..WRW.",
        "WRRRWWRRRW",
        ".WRWRRWRW.",
        "..WRRRRW..",
        "...WRRW...",
    ];

    // ---------------------------------------------------------------- scenery

    public static readonly string[] Torch =
    [
        "..Y..",
        ".YRY.",
        ".RYR.",
        "..R..",
        "WWWWW",
        ".WWW.",
        "..W..",
        "..W..",
        "..W..",
        "..W..",
        ".WWW.",
    ];

    public static readonly string[] TorchFlicker =
    [
        ".Y...",
        "..YR.",
        ".RYR.",
        "..R..",
        "WWWWW",
        ".WWW.",
        "..W..",
        "..W..",
        "..W..",
        "..W..",
        ".WWW.",
    ];

    public static readonly string[] Window =
    [
        "...WWWW...",
        "..WBBBBW..",
        ".WBBBBBBW.",
        "WBBBBWBBBW",
        "WBBBBWBBBW",
        "WBBBBWBBBW",
        "WWWWWWWWWW",
        "WBBBBWBBBW",
        "WBBBBWBBBW",
        "WBCBBWBBBW",
        "WBBBBWBBCW",
        "WBBBBWBBBW",
        "WWWWWWWWWW",
    ];

    public static readonly string[] Shield =
    [
        "WWWWWWWW",
        "WRRRWRRW",
        "WRRRWRRW",
        "WWWWWWWW",
        "WRRWRRRW",
        "WRRWRRRW",
        ".WRWRRW.",
        "..WWRW..",
        "...WW...",
    ];

    public static readonly string[] Tapestry =
    [
        "YYYYYYYYYY",
        "RRRRRRRRRR",
        "RRRRYYRRRR",
        "RRRYYYYRRR",
        "RRYYRRYYRR",
        "RRRYYYYRRR",
        "RRRRYYRRRR",
        "RRRRRRRRRR",
        "RRYRRRRYRR",
        "RRRRRRRRRR",
        "RRRRYYRRRR",
        "RRRYRRYRRR",
        "RRRRYYRRRR",
        "RRRRRRRRRR",
        "Y.Y.Y.Y.Y.",
    ];

    public static readonly string[] Books =
    [
        "YYYYYYYYYYYYYYYY",
        "YRRGBBMRGCRRBGMY",
        "YRRGBBMRGCRRBGMY",
        "YRRGBBMRGCRRBGMY",
        "YYYYYYYYYYYYYYYY",
        "YBGRRMCCBRGGMRBY",
        "YBGRRMCCBRGGMRBY",
        "YBGRRMCCBRGGMRBY",
        "YYYYYYYYYYYYYYYY",
        "YCMBGRRGMBCCRGRY",
        "YCMBGRRGMBCCRGRY",
        "YCMBGRRGMBCCRGRY",
        "YYYYYYYYYYYYYYYY",
        "Y..............Y",
    ];

    public static readonly string[] Barrels =
    [
        ".RRRRR...RRRRR..",
        "RYRRRYR.RYRRRYR.",
        "RYRRRYR.RYRRRYR.",
        "YYYYYYY.YYYYYYY.",
        "RYRRRYR.RYRRRYR.",
        "RYRRRYR.RYRRRYR.",
        "YYYYYYY.YYYYYYY.",
        "RYRRRYR.RYRRRYR.",
        ".RRRRR...RRRRR..",
    ];

    public static readonly string[] Altar =
    [
        "..Y.........Y...",
        "..W....Y....W...",
        "..W...YYY...W...",
        "..W....Y....W...",
        "WWWWWWWWWWWWWWWW",
        "WRRRRRRRRRRRRRRW",
        "WRRYRRRRRRRRYRRW",
        "WRRRRRRRRRRRRRRW",
        "WRRRRRRRRRRRRRRW",
        "WWWWWWWWWWWWWWWW",
    ];

    public static readonly string[] Throne =
    [
        "Y....YY....Y",
        "YY..YYYY..YY",
        "YRRRRRRRRRRY",
        "YRRRRRRRRRRY",
        "YRRRRRRRRRRY",
        "YRRRRRRRRRRY",
        "YRRRRRRRRRRY",
        "YRRRRRRRRRRY",
        "YYYYYYYYYYYY",
        "YRRRRRRRRRRY",
        "YYYYYYYYYYYY",
        "Y.Y......Y.Y",
        "Y.Y......Y.Y",
        "Y.Y......Y.Y",
    ];

    public static readonly string[] Bed =
    [
        "Y..................Y",
        "YWW................Y",
        "YWWWMMMMMMMMMMMMMMMY",
        "YMMMMMMMMMMMMMMMMMMY",
        "YMMMMMMMMMMMMMMMMMMY",
        "YYYYYYYYYYYYYYYYYYYY",
        "Y..................Y",
        "Y..................Y",
    ];

    public static readonly string[] Chains =
    [
        "..W...",
        ".W.W..",
        "..W...",
        ".W.W..",
        "..W...",
        ".W.W..",
        "..W...",
        ".W.W..",
        "..W...",
        ".WWW..",
        "W...W.",
        "W...W.",
        ".WWW..",
    ];

    public static readonly string[] Cobweb =
    [
        "WWWWWWWWWW",
        "W.W..W..W.",
        "WW.W.W.W..",
        "W.WWWWW...",
        "W.W.WW....",
        "WW.WW.....",
        "W.WW......",
        "WWW.......",
        "WW........",
        "W.........",
    ];

    public static readonly string[] Bones =
    [
        "....WWW.....",
        "...WKWKW....",
        "...WWWWW.W.W",
        "....WWW...W.",
        "W.W.....WWWW",
        ".WWWWWWWW..W",
    ];

    public static readonly string[] Cauldron =
    [
        "....G..G....",
        "..G..GG...G.",
        "WWWWWWWWWWWW",
        ".WGGGGGGGGW.",
        "WWWWWWWWWWWW",
        ".WWWWWWWWWW.",
        ".WWWWWWWWWW.",
        "..WWWWWWWW..",
        "...RYRRYR...",
        "..RYRYYRYR..",
    ];

    public static readonly string[] Table =
    [
        "..W....Y.R......",
        ".WWW..YYYRR.....",
        "YYYYYYYYYYYYYYYY",
        "RRRRRRRRRRRRRRRR",
        ".R............R.",
        ".R............R.",
        ".R............R.",
        ".R............R.",
    ];

    public static readonly string[] Portrait =
    [
        "YYYYYYYYYY",
        "YBBBBBBBBY",
        "YBBBWWBBBY",
        "YBBWWWWBBY",
        "YBBWKWKBBY",
        "YBBWWWWBBY",
        "YBBBWWBBBY",
        "YBRRRRRRBY",
        "YRRRRRRRRY",
        "YRRRRRRRRY",
        "YYYYYYYYYY",
    ];

    // ---------------------------------------------------------------- title

    /// <summary>
    /// Left half of the heraldic eagle on the title screen; the last column is the centre line.
    /// The picture is mirrored to make the right half.
    /// </summary>
    public static readonly string[] TitleEagleHalf =
    [
        "Y.......................",
        "YY......................",
        "YYY.....................",
        ".YYY....................",
        ".YYYY...................",
        "..YYYY..............YYYY",
        "..YYYYY............YYYYY",
        "...YYYYYY.........YYRYYY",
        "...YYYYYYY........YYYYYY",
        "....YYYYYYYY.......YYYRR",
        "....YYYYYYYYYY......YYRR",
        ".....YYYYYYYYYYY...YYYYY",
        ".....YWYYYYYYYYYYYYYYYYY",
        "......YWYYYYYYYYYYYYYYYY",
        "......Y.YWYYYYYYYYYYYYYY",
        ".......Y.Y.YWYYYYYYYYYYY",
        "..........Y.Y.YYYYYYYYYY",
        "...............YYYYYYYYY",
        "................YYYYYYYY",
        ".................YYYYYYY",
        "..................YYYYYY",
        "..............RR...YYYYY",
        "...............RRRRYYYYY",
        "..................YYYYYY",
        ".................YYYYYYY",
        "................YYY.YYYY",
        "................Y.Y.Y.YY",
    ];

    /// <summary>The whole title eagle (declared after its half so it is built from it).</summary>
    public static readonly string[] TitleEagle = Mirror(TitleEagleHalf);

    private static string[] Mirror(string[] halves)
    {
        var rows = new string[halves.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            string half = halves[i];
            var right = half.Substring(0, half.Length - 1).ToCharArray();
            System.Array.Reverse(right);
            rows[i] = half + new string(right);
        }
        return rows;
    }
}
