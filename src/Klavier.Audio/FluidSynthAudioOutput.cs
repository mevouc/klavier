using Klavier.Audio.Options;
using Klavier.Core.Events;
using Klavier.Core.Ports;
using Microsoft.Extensions.Options;
using NFluidsynth;

namespace Klavier.Audio;

public class FluidSynthAudioOutput(
    IOptionsMonitor<AudioConfig> audioConfig)
    : IAudioOutput
{
    private const int _MidiChannel = 0;
    private readonly Settings _synthSettings = new();
    private Synth? _synth;
    private AudioDriver? _audioDriver;

    private bool isDisposed;

    public static void ConfigureLogging()
    {
        Logger.SetLoggerMethod((level, message, _) =>
        {
            if (level <= Logger.LogLevel.Error)
            {
                Console.Error.WriteLine($"FluidSynth ({level}): {message}");
            }
        });
    }

    public void Initialize()
    {
        _synthSettings[ConfigurationKeys.AudioDriver].StringValue = audioConfig.CurrentValue.AudioDriver;

        _synth = new(_synthSettings);
        _synth.LoadSoundFont(audioConfig.CurrentValue.SoundFontPath, true);

        _audioDriver = new(_synthSettings, _synth);
    }

    public void OnNoteOn(NoteOnEvent noteOnEvent)
    {
        _synth?.NoteOn(_MidiChannel, noteOnEvent.Pitch, noteOnEvent.Velocity);
    }

    public void OnNoteOff(NoteOffEvent noteOffEvent)
    {
        _synth?.NoteOff(_MidiChannel, noteOffEvent.Pitch);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                _audioDriver?.Dispose();
                _synth?.Dispose();
                _synthSettings.Dispose();
            }

            _audioDriver = null;
            _synth = null;
            isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
