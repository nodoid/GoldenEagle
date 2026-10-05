using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GoldenEagle.Core.Audio;
using GoldenEagle.Core.Engine;
using GoldenEagle.Core.Graphics;
using GoldenEagle.Core.Input;
using GoldenEagle.Core.Localization;
using GoldenEagle.Core.Oric;
using GoldenEagle.Core.Platform;
using GoldenEagle.Core.Scenes;
using GoldenEagle.Core.Session;

namespace GoldenEagle.Core;

/// <summary>
/// The Golden Eagle (L'Aigle d'Or), by PFJ, based on the 1984 Loriciels game by Louis-Marie Rocques.
/// Laid out on the Oric's 240x224 screen but drawn at the full resolution of the display, in
/// French or English.
/// </summary>
public class GoldenEagleGame : Game
{
    public const int TicksPerSecond = Adventure.TicksPerSecond;

    private readonly GraphicsDeviceManager _graphics;
    private RenderTarget2D _target;
    private SpriteBatch _present;
    private Rectangle _destination;
    private Scene _scene;
    private Scene _pending;
    private bool _paused;
    private KeyboardState _prevKeys;

    public GoldenEagleGame() : this(null)
    {
    }

    public GoldenEagleGame(IPlatformServices platform)
    {
        Platform = platform ?? new DesktopPlatformServices();
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / TicksPerSecond);

        if (Platform.IsMobile)
        {
            _graphics.IsFullScreen = true;
            _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = Canvas.Width * 4;
            _graphics.PreferredBackBufferHeight = Canvas.Height * 4;
            IsMouseVisible = true;
        }
    }

    public IPlatformServices Platform { get; }
    public Canvas Canvas { get; private set; }
    public InputManager Input { get; private set; }
    public SoundBank Sound { get; private set; }
    public HighScoreTable HighScores { get; private set; }
    public GameSettings Settings { get; private set; }

    /// <summary>The game in progress, if any (kept so the menu can continue it).</summary>
    public Adventure Current { get; private set; }

    public bool IsMobile => Platform.IsMobile || (Capture?.Mobile ?? false);

    /// <summary>Store-asset capture in progress (see <see cref="Demo.CaptureDirector"/>).</summary>
    public Demo.CaptureDirector Capture { get; set; }

    internal void ShowCaptureScene(Scene scene) => _scene = scene;

    protected override void Initialize()
    {
        Window.Title = "The Golden Eagle - L'Aigle d'Or";
        if (!IsMobile)
        {
            Window.AllowUserResizing = true;
            FitWindowToDisplay();
        }
        base.Initialize();
    }

    private void FitWindowToDisplay()
    {
        var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        int scale = 4;
        while (scale > 1 && (Canvas.Width * scale > mode.Width * 0.9 || Canvas.Height * scale > mode.Height * 0.85))
            scale--;
        _graphics.PreferredBackBufferWidth = Canvas.Width * scale;
        _graphics.PreferredBackBufferHeight = Canvas.Height * scale;
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        _present = new SpriteBatch(GraphicsDevice);
        Canvas = new Canvas(GraphicsDevice);
        Input = new InputManager(IsMobile);
        Sound = new SoundBank();
        Settings = GameSettings.Load(Platform.DataDirectory, Platform.SystemLanguage);
        Strings.Current = Capture?.Language ?? Settings.Language;
        for (int i = 0; i < Settings.Volume; i++)
            Sound.CycleVolume();
        Sound.Muted = Capture != null;
        HighScores = new HighScoreTable(Platform.DataDirectory);
        HighScores.Load();
        _scene = new TitleScene(this);
        _scene.Enter();
    }

    protected override void UnloadContent()
    {
        Sound?.Dispose();
        Canvas?.Dispose();
        _target?.Dispose();
        _present?.Dispose();
        base.UnloadContent();
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        if (_scene is IPausable)
            _paused = true;
        base.OnDeactivated(sender, args);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Capture != null)
        {
            IsFixedTimeStep = false;
            Capture.Update(this);
            base.Update(gameTime);
            return;
        }
        UpdateDestination();
        Input.Update(ToVirtual, IsActive);

        var keys = Keyboard.GetState();
        bool altEnter = keys.IsKeyDown(Keys.Enter) && !_prevKeys.IsKeyDown(Keys.Enter) &&
                        (keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt));
        if (!IsMobile && (altEnter || (keys.IsKeyDown(Keys.F11) && !_prevKeys.IsKeyDown(Keys.F11))))
            _graphics.ToggleFullScreen();
        _prevKeys = keys;

        if (_pending != null)
        {
            _scene.Leave();
            _scene = _pending;
            _pending = null;
            _paused = false;
            _scene.Enter();
        }

        if (_scene is IPausable)
        {
            if (Input.PausePressed)
                _paused = !_paused;
            if (_paused)
            {
                if (Input.ConfirmPressed || Input.Taps.Count > 0)
                    _paused = false;
                else if (Input.BackPressed)
                {
                    _paused = false;
                    ShowMenu();
                }
                base.Update(gameTime);
                return;
            }
        }

        _scene.Tick();
        base.Update(gameTime);
    }

    /// <summary>Largest render scale used on screen (device pixels per virtual pixel).</summary>
    public const float MaxScale = 8f;

    /// <summary>Makes sure the render target matches the resolution the frame is drawn at.</summary>
    private float PrepareTarget()
    {
        UpdateDestination();
        float scale = Capture?.Scale ?? Math.Clamp((float)_destination.Width / Canvas.Width, 1f, MaxScale);
        int w = (int)MathF.Round(Canvas.Width * scale), h = (int)MathF.Round(Canvas.Height * scale);
        if (_target == null || _target.Width != w || _target.Height != h)
        {
            _target?.Dispose();
            // Multisampled targets smooth the lines on desktop; MonoGame's mobile GL backend can't resolve them.
            _target = new RenderTarget2D(GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None,
                Platform.IsMobile ? 0 : 4, RenderTargetUsage.DiscardContents);
        }
        return scale;
    }

    protected override void Draw(GameTime gameTime)
    {
        float scale = PrepareTarget();
        GraphicsDevice.SetRenderTarget(_target);
        GraphicsDevice.Clear(Color.Black);
        Canvas.Begin(scale);
        _scene.Draw(Canvas);
        if (_paused && Capture == null)
        {
            Canvas.Fill(78, 70, 84, 26, OricColor.Blue);
            Canvas.TextCentered(74, Strings.Paused, OricColor.Yellow);
            Canvas.TextCentered(85, Strings.Resume(IsMobile), OricColor.White);
        }
        Canvas.End();
        GraphicsDevice.SetRenderTarget(null);
        Capture?.AfterDraw(_target);

        GraphicsDevice.Clear(Color.Black);
        _present.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);
        _present.Draw(_target, _destination, Color.White);
        _present.End();
        base.Draw(gameTime);
    }

    private void UpdateDestination()
    {
        var pp = GraphicsDevice.PresentationParameters;
        _destination = ComputeDestination(pp.BackBufferWidth, pp.BackBufferHeight, false);
    }

    /// <summary>Largest aspect-correct rectangle for the Oric screen, integer scaled when it fits.</summary>
    public static Rectangle ComputeDestination(int backWidth, int backHeight, bool preferInteger)
    {
        float scale = Math.Min((float)backWidth / Canvas.Width, (float)backHeight / Canvas.Height);
        if (preferInteger && scale >= 1f)
            scale = (float)Math.Floor(scale);
        int w = Math.Max(1, (int)(Canvas.Width * scale));
        int h = Math.Max(1, (int)(Canvas.Height * scale));
        return new Rectangle((backWidth - w) / 2, (backHeight - h) / 2, w, h);
    }

    /// <summary>Converts window / touch coordinates into Oric pixels.</summary>
    public Vector2 ToVirtual(Vector2 screen)
    {
        if (_destination.Width == 0 || _destination.Height == 0)
            return new Vector2(-1, -1);
        return new Vector2(
            (screen.X - _destination.X) * Canvas.Width / _destination.Width,
            (screen.Y - _destination.Y) * Canvas.Height / _destination.Height);
    }

    // ---------------- flow ----------------

    public void SwitchTo(Scene scene) => _pending = scene;

    public void ShowTitle() => SwitchTo(new TitleScene(this));

    public void ShowMenu() => SwitchTo(new MenuScene(this));

    public void ShowInstructions() => SwitchTo(new InstructionsScene(this));

    public void ShowHallOfFame(Adventure finished = null) => SwitchTo(new HallOfFameScene(this, finished));

    public void ShowDemo() => SwitchTo(new Demo.DemoScene(this, Sound, attract: true, seed: Environment.TickCount));

    public void StartNewGame()
    {
        Current = new Adventure(Sound, Environment.TickCount);
        Sound.Play(Sfx.Start);
        SwitchTo(new GameScene(this, Current));
    }

    public void GameEnded(Adventure adventure)
    {
        Current = null;
        SwitchTo(new EndScene(this, adventure));
    }

    public void ToggleLanguage()
    {
        Strings.Current = Strings.Current == Language.French ? Language.English : Language.French;
        Settings.Language = Strings.Current;
        Settings.Save();
    }

    public void CycleVolume()
    {
        Sound.CycleVolume();
        Settings.Volume = (Settings.Volume + 1) % 4;
        Settings.Save();
    }

    public void Quit() => Platform.Quit(this);
}

/// <summary>Marks scenes that may be paused (gameplay).</summary>
public interface IPausable
{
}

/// <summary>Player preferences persisted between runs.</summary>
public sealed class GameSettings
{
    private const string FileName = "settings.txt";
    private string _directory;

    public Language Language { get; set; }

    /// <summary>Volume step index as used by <see cref="SoundBank.CycleVolume"/>.</summary>
    public int Volume { get; set; }

    /// <summary>Loads the settings; the first run takes its language from the device.</summary>
    public static GameSettings Load(string directory, string systemLanguage)
    {
        var s = new GameSettings { _directory = directory, Language = Strings.FromIso(systemLanguage) };
        try
        {
            string path = Path.Combine(directory, FileName);
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    var kv = line.Split('=');
                    if (kv.Length != 2)
                        continue;
                    if (kv[0] == "language")
                        s.Language = Strings.FromIso(kv[1].Trim());
                    else if (kv[0] == "volume" && int.TryParse(kv[1], out int v))
                        s.Volume = Math.Clamp(v, 0, 3);
                }
            }
        }
        catch (Exception)
        {
        }
        return s;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, FileName),
                $"language={Strings.IsoCode(Language)}\nvolume={Volume}\n");
        }
        catch (Exception)
        {
        }
    }
}
