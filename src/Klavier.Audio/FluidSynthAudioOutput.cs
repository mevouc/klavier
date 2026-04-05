using Klavier.Audio.Options;
using Klavier.Core.Events;
using Klavier.Core.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFluidsynth;

namespace Klavier.Audio;

public class FluidSynthAudioOutput : IAudioOutput
{
    private const int _MidiChannel = 0;
    private readonly Settings _synthSettings;
    private readonly IOptionsMonitor<AudioConfig> _audioConfig;
    private AudioConfig _lastAudioConfig;
    private Synth? _synth;
    private AudioDriver? _audioDriver;

    private bool _isDisposed;

    public FluidSynthAudioOutput(
        IOptionsMonitor<AudioConfig> audioConfig,
        ILogger<FluidSynthAudioOutput> logger)
    {
        _audioConfig = audioConfig;

        _lastAudioConfig = _audioConfig.CurrentValue;
        _audioConfig.OnChange(OnAudioConfigChanged); // dynamically update volume/gain

        ConfigureThirdPartyLogging(logger);
        _synthSettings = new Settings();
    }

    private void ConfigureThirdPartyLogging(ILogger<FluidSynthAudioOutput> logger)
    {
        Logger.LogLevel minimumLogLevel = _audioConfig.CurrentValue.FluidSynthLogLevel;

        Logger.SetLoggerMethod((level, message, _) =>
        {
            if (level <= minimumLogLevel)
            {
                switch (level)
                {
                    case Logger.LogLevel.Panic:
                    case Logger.LogLevel.Error:
                        logger.LogError("FluidSynth ({Level}): {Message}", level, message);
                        break;
                    case Logger.LogLevel.Warning:
                        logger.LogWarning("FluidSynth ({Level}): {Message}", level, message);
                        break;
                    case Logger.LogLevel.Information:
                        logger.LogInformation("FluidSynth ({Level}): {Message}", level, message);
                        break;
                    default:
                        logger.LogDebug("FluidSynth ({Level}): {Message}", level, message);
                        break;
                }
            }
        });
    }

    public void Initialize()
    {
        _synthSettings[ConfigurationKeys.AudioDriver].StringValue = _audioConfig.CurrentValue.AudioDriver;
        _synthSettings[ConfigurationKeys.SynthGain].DoubleValue = _audioConfig.CurrentValue.GainFactor;

        _synth = new(_synthSettings);
        _synth.LoadSoundFont(_audioConfig.CurrentValue.SoundFontPath, true);

        _audioDriver = new(_synthSettings, _synth);
    }

    public void OnNoteOn(NoteOnEvent noteOnEvent)
    {
        _synth?.NoteOn(_MidiChannel, noteOnEvent.Pitch.Value, noteOnEvent.Velocity.Value);
    }

    public void OnNoteOff(NoteOffEvent noteOffEvent)
    {
        _synth?.NoteOff(_MidiChannel, noteOffEvent.Pitch.Value);
    }

    private void OnAudioConfigChanged(AudioConfig newConfig)
    {
        if (newConfig.VolumeInPercent != _lastAudioConfig.VolumeInPercent)
        {
            _synth?.Gain = newConfig.GainFactor;
        }
        _lastAudioConfig = newConfig;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _audioDriver?.Dispose();
                _synth?.Dispose();
                _synthSettings.Dispose();
            }

            _audioDriver = null;
            _synth = null;
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
