using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace GoldenEagle.Core.Audio;

/// <summary>Builds every sound effect at start-up from <see cref="Synth"/> and plays them.</summary>
public sealed class SoundBank : ISoundPlayer, IDisposable
{
    private readonly Dictionary<Sfx, SoundEffect> _effects = new();
    private readonly bool _enabled;
    private static readonly float[] VolumeSteps = [0.6f, 0.3f, 0f, 1f];
    private SoundEffect _tune;
    private SoundEffectInstance _tuneInstance;
    private int _volumeIndex;

    public SoundBank()
    {
        try
        {
            foreach (var (id, pcm) in Build())
                _effects[id] = new SoundEffect(Synth.ToBytes(pcm), Synth.SampleRate, AudioChannels.Mono);
            _tune = new SoundEffect(Synth.ToBytes(TitleTune()), Synth.SampleRate, AudioChannels.Mono);
            _enabled = true;
        }
        catch (Exception)
        {
            // No audio device (e.g. headless CI): run silently.
            _enabled = false;
        }
    }

    public float Volume => VolumeSteps[_volumeIndex];

    /// <summary>0 off, 1 low, 2 medium, 3 high.</summary>
    public int Level => Volume <= 0f ? 0 : Volume < 0.5f ? 1 : Volume < 0.9f ? 2 : 3;

    /// <summary>Cycles the volume. Returns the new level 0..3 (0 = mute).</summary>
    public int CycleVolume()
    {
        _volumeIndex = (_volumeIndex + 1) % VolumeSteps.Length;
        Play(Sfx.Menu);
        return (int)Math.Round(Volume * 3);
    }

    public void Play(Sfx sfx, float pitch = 0f)
    {
        if (!_enabled || Volume <= 0f)
            return;
        if (_effects.TryGetValue(sfx, out var e))
        {
            try
            {
                e.Play(Volume, Math.Clamp(pitch, -1f, 1f), 0f);
            }
            catch (Exception)
            {
            }
        }
    }

    internal static IEnumerable<(Sfx, short[])> Build()
    {
        yield return (Sfx.Step, Synth.Noise(0.025, 0.25, 0, 14));
        yield return (Sfx.Take, Synth.Tune(0.4, (784, 0.05), (1047, 0.08)));
        yield return (Sfx.Drop, Synth.Tune(0.35, (523, 0.05), (392, 0.08)));
        yield return (Sfx.Door, Synth.Mix(Synth.Sweep(0.3, 140, 90, 0.35, 0.1), Synth.Noise(0.3, 0.2, 0, 20)));
        yield return (Sfx.Stairs, Synth.Concat(Synth.Noise(0.05, 0.3, 0, 10), Synth.Silence(0.05),
            Synth.Noise(0.05, 0.3, 0, 10), Synth.Silence(0.05), Synth.Noise(0.05, 0.3, 0, 10)));
        yield return (Sfx.Unlock, Synth.Concat(Synth.Noise(0.05, 0.5, 0, 4), Synth.Tune(0.4, (659, 0.07), (988, 0.15))));
        yield return (Sfx.Locked, Synth.Tune(0.35, (196, 0.08), (0, 0.04), (196, 0.12)));
        yield return (Sfx.Fall, Synth.Concat(Synth.Sweep(0.8, 1400, 110, 0.35, 0.25), Synth.Noise(0.25, 0.6, 0, 6)));
        yield return (Sfx.Hurt, Synth.Mix(Synth.Noise(0.2, 0.5, 0, 3), Synth.Sweep(0.2, 300, 120, 0.3, 0)));
        yield return (Sfx.Ghost, Synth.Concat(Synth.Sweep(0.35, 300, 600, 0.25, 0.25), Synth.Sweep(0.35, 600, 250, 0.25, 0)));
        yield return (Sfx.Drink, Synth.Concat(Synth.Sweep(0.08, 400, 800, 0.3, 0.3), Synth.Sweep(0.08, 400, 900, 0.3, 0.3),
            Synth.Tune(0.35, (1047, 0.08), (1319, 0.16))));
        yield return (Sfx.Poison, Synth.Concat(Synth.Sweep(0.08, 400, 800, 0.3, 0.3), Synth.Sweep(0.6, 500, 60, 0.4, 0.1)));
        yield return (Sfx.Eat, Synth.Concat(Synth.Noise(0.06, 0.4, 0, 6), Synth.Silence(0.06), Synth.Noise(0.06, 0.4, 0, 6)));
        yield return (Sfx.Treasure, Synth.Tune(0.4, (1047, 0.06), (1319, 0.06), (1568, 0.06), (2093, 0.18)));
        yield return (Sfx.Eagle, Synth.Tune(0.45, (523, 0.1), (659, 0.1), (784, 0.1), (1047, 0.1), (784, 0.1), (1047, 0.35)));
        yield return (Sfx.Victory, Synth.Tune(0.45, (392, 0.15), (523, 0.15), (659, 0.15), (784, 0.3), (659, 0.15),
            (784, 0.15), (1047, 0.6)));
        yield return (Sfx.Death, Synth.Tune(0.4, (392, 0.25), (370, 0.25), (349, 0.25), (330, 0.6)));
        yield return (Sfx.Menu, Synth.Sweep(0.05, 900, 1200, 0.3, 0.1));
        yield return (Sfx.Start, Synth.Tune(0.4, (262, 0.12), (330, 0.12), (392, 0.12), (523, 0.3)));
        yield return (Sfx.Warning, Synth.Tune(0.35, (880, 0.08), (0, 0.06), (880, 0.08)));
    }

    /// <summary>The title tune: a little modal fanfare in the style of the Oric's sound chip.</summary>
    internal static short[] TitleTune()
    {
        const double q = 0.18;
        return Synth.Tune(0.32,
            (440, q), (523, q), (587, q), (659, q * 2), (587, q), (523, q), (440, q * 2),
            (392, q), (440, q), (523, q), (440, q * 3), (0, q),
            (659, q), (784, q), (880, q * 2), (784, q), (659, q), (587, q * 2),
            (523, q), (587, q), (659, q), (440, q * 4));
    }

    /// <summary>Plays the title tune once (restarting it if it is already playing).</summary>
    public void PlayTune()
    {
        if (!_enabled || Volume <= 0f || _tune == null)
            return;
        try
        {
            _tuneInstance?.Stop();
            _tuneInstance ??= _tune.CreateInstance();
            _tuneInstance.Volume = Volume;
            _tuneInstance.Play();
        }
        catch (Exception)
        {
        }
    }

    public void StopTune()
    {
        try
        {
            _tuneInstance?.Stop();
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        _tuneInstance?.Dispose();
        _tune?.Dispose();
        foreach (var e in _effects.Values)
            e.Dispose();
        _effects.Clear();
    }
}
