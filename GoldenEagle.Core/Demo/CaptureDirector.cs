using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using GoldenEagle.Core.Audio;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Scenes;

namespace GoldenEagle.Core.Demo;

/// <summary>
/// Store asset capture: plays a list of scenes, saving native 240x224 frames as PNG (stills,
/// or every tick for videos) and recording the sound effects so a matching audio track can be
/// mixed. Start with <c>--capture &lt;folder&gt; [stills|videos|all] [--mobile] [--lang fr|en] [--scale n]</c>.
/// </summary>
public sealed class CaptureDirector : ISoundPlayer
{
    /// <summary>
    /// One capture: the scene to make, how long to run it before saving, how many frames to save,
    /// and (for the castle) the moment to wait for.
    /// </summary>
    public sealed record Shot(string Name, Func<GoldenEagleGame, ISoundPlayer, Scene> Create, int SkipTicks,
        int Frames = 1, Func<Adventure, bool> Ready = null);

    private readonly string _folder;
    private readonly List<Shot> _shots;
    private int _index = -1;
    private Scene _scene;
    private int _frame;
    private bool _saveThisFrame;
    private readonly List<(int frame, Sfx sfx)> _events = new();

    public CaptureDirector(string folder, List<Shot> shots, bool mobile, Language language, float scale)
    {
        Scale = scale;
        _folder = folder;
        _shots = shots;
        Mobile = mobile;
        Language = language;
    }

    public bool Mobile { get; }

    /// <summary>Capture resolution in device pixels per virtual pixel (10 gives 2400 x 2240).</summary>
    public float Scale { get; }
    public Language Language { get; }
    public bool Finished { get; private set; }

    public static CaptureDirector FromArgs(string[] args)
    {
        int i = Array.IndexOf(args, "--capture");
        if (i < 0 || i + 1 >= args.Length)
            return null;
        string folder = args[i + 1];
        string what = i + 2 < args.Length && !args[i + 2].StartsWith("--") ? args[i + 2] : "all";
        bool mobile = Array.IndexOf(args, "--mobile") >= 0;
        int l = Array.IndexOf(args, "--lang");
        var language = l >= 0 && l + 1 < args.Length ? Strings.FromIso(args[l + 1]) : Language.English;
        var shots = new List<Shot>();
        if (what is "stills" or "all")
            shots.AddRange(Stills());
        if (what is "videos" or "all")
            shots.AddRange(Videos());
        int sc = Array.IndexOf(args, "--scale");
        float scale = sc >= 0 && sc + 1 < args.Length && float.TryParse(args[sc + 1], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float s) ? s : what == "videos" ? 5f : 10f;
        return new CaptureDirector(folder, shots, mobile, language, scale);
    }

    private static Scene Castle(GoldenEagleGame g, ISoundPlayer s) => new DemoScene(g, s, attract: false);

    private static Func<Adventure, bool> In(string room, int after = 40) => a => a.RoomId == room && a.RoomTicks >= after;

    /// <summary>The still screenshots, in store order.</summary>
    public static IEnumerable<Shot> Stills()
    {
        yield return new Shot("01-gatehouse", Castle, 0, Ready: a => a.RoomId == "gatehouse" && a.Ticks >= 70);
        yield return new Shot("02-title", (g, s) => new TitleScene(g), 100);
        yield return new Shot("03-dungeon", Castle, 0, Ready: a => a.RoomId == "dungeon" && a.Has(World.ItemKind.Key) && a.RoomTicks > 30);
        yield return new Shot("04-chapel", Castle, 0, Ready: In("chapel", 50));
        yield return new Shot("05-throne", Castle, 0, Ready: In("throne", 60));
        yield return new Shot("06-catacombs", Castle, 0, Ready: In("catacombs", 70));
        yield return new Shot("07-eyrie", Castle, 0, Ready: In("eyrie", 14));
        yield return new Shot("08-library", Castle, 0, Ready: In("library", 40));
        yield return new Shot("09-victory", (g, s) => VictoryScene(g, s), 30);
        yield return new Shot("10-menu", (g, s) => new MenuScene(g), 30);
        yield return new Shot("11-laboratory", Castle, 0, Ready: In("laboratory", 50));
        yield return new Shot("12-instructions", (g, s) => new InstructionsScene(g), 5);
        yield return new Shot("13-guardroom", Castle, 0, Ready: In("guardroom", 30));
        yield return new Shot("14-crypt", Castle, 0, Ready: In("crypt", 40));
    }

    /// <summary>One continuous montage of the game in action (about 30 seconds at 50 frames a second).</summary>
    public static IEnumerable<Shot> Videos()
    {
        yield return new Shot("video/01", (g, s) => new TitleScene(g), 20, 150);
        yield return new Shot("video/02", Castle, 0, 150, a => a.Ticks >= 20);
        yield return new Shot("video/03", Castle, 0, 160, In("dungeon", 1));
        yield return new Shot("video/04", Castle, 0, 160, In("chapel", 1));
        yield return new Shot("video/05", Castle, 0, 160, In("throne", 1));
        yield return new Shot("video/06", Castle, 0, 160, In("laboratory", 1));
        yield return new Shot("video/07", Castle, 0, 180, In("catacombs", 1));
        yield return new Shot("video/08", Castle, 0, 200, In("eyrie", 1));
        yield return new Shot("video/09", (g, s) => VictoryScene(g, s), 0, 150);
    }

    private static Scene VictoryScene(GoldenEagleGame g, ISoundPlayer s)
    {
        var demo = new DemoScene(g, SilentSoundPlayer.Instance, attract: false);
        demo.FastForward(a => a.Outcome != Outcome.Playing);
        return new EndScene(g, demo.Adventure);
    }

    public void Play(Sfx sfx, float pitch = 0f)
    {
        if (_index >= 0 && _shots[_index].Frames > 1 && _recording)
            _events.Add((_frame, sfx));
    }

    private bool _recording;

    /// <summary>Called instead of the normal update while capturing.</summary>
    public void Update(GoldenEagleGame game)
    {
        if (Finished)
            return;
        if (_scene == null)
        {
            if (++_index >= _shots.Count)
            {
                Finished = true;
                game.Exit();
                return;
            }
            var shot0 = _shots[_index];
            _recording = false;
            _scene = shot0.Create(game, this);
            _scene.Enter();
            game.ShowCaptureScene(_scene);
            for (int i = 0; i < shot0.SkipTicks; i++)
                _scene.Tick();
            if (shot0.Ready != null && _scene is DemoScene demo)
                demo.FastForward(shot0.Ready);
            _frame = 0;
            _events.Clear();
            _recording = true;
        }

        _scene.Tick();
        _saveThisFrame = true;
    }

    /// <summary>Called after the Oric screen has been drawn.</summary>
    public void AfterDraw(RenderTarget2D target)
    {
        if (!_saveThisFrame || _index < 0 || _index >= _shots.Count)
            return;
        _saveThisFrame = false;
        var shot = _shots[_index];
        string path = shot.Frames > 1
            ? Path.Combine(_folder, shot.Name, $"f{_frame:D5}.png")
            : Path.Combine(_folder, "stills", shot.Name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var file = File.Create(path))
            target.SaveAsPng(file, target.Width, target.Height);
        _frame++;
        if (_frame >= shot.Frames)
        {
            if (shot.Frames > 1)
                WriteAudio(Path.Combine(_folder, shot.Name + ".wav"), shot.Frames, shot.Name == "video/01");
            Console.WriteLine($"captured {shot.Name}");
            _scene.Leave();
            _scene = null;
        }
    }

    private void WriteAudio(string path, int frames, bool tune)
    {
        var clips = new Dictionary<Sfx, short[]>();
        foreach (var (id, pcm) in SoundBank.Build())
            clips[id] = pcm;
        int total = frames * Synth.SampleRate / GoldenEagleGame.TicksPerSecond;
        var mix = new int[total];
        void Add(short[] clip, int start)
        {
            for (int i = 0; i < clip.Length && start + i < total; i++)
                mix[start + i] += clip[i] * 6 / 10;
        }
        if (tune)
            Add(SoundBank.TitleTune(), 0);
        foreach (var (frame, sfx) in _events)
            Add(clips[sfx], frame * Synth.SampleRate / GoldenEagleGame.TicksPerSecond);
        var samples = new short[total];
        for (int i = 0; i < total; i++)
            samples[i] = (short)Math.Clamp(mix[i], short.MinValue, short.MaxValue);
        Wav.Write(path, samples, Synth.SampleRate);
    }
}

public static class Wav
{
    /// <summary>Writes 16-bit mono PCM as a .wav file.</summary>
    public static void Write(string path, short[] samples, int rate)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var w = new BinaryWriter(File.Create(path));
        int bytes = samples.Length * 2;
        w.Write("RIFF"u8.ToArray());
        w.Write(36 + bytes);
        w.Write("WAVE"u8.ToArray());
        w.Write("fmt "u8.ToArray());
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(rate);
        w.Write(rate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8.ToArray());
        w.Write(bytes);
        foreach (var s in samples)
            w.Write(s);
    }
}
