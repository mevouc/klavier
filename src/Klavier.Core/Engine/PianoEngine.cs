using Klavier.Core.Events;
using Klavier.Core.Options;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.Core.Engine;

public class PianoEngine : IPianoEngine
{
    private readonly IOptionsMonitor<PlaybackConfig> _playbackConfig;
    private readonly ILogger<PianoEngine> _logger;
    private PlaybackConfig _lastPlaybackConfig;
    private readonly Dictionary<NotePitch, int> _activeNotes = []; // value is active inputs count (note plays when there's at least one)
    private readonly HashSet<INoteEventHandler> _noteEventHandlers = [];

    public PianoEngine(
        IOptionsMonitor<PlaybackConfig> playbackConfig,
        ILogger<PianoEngine> logger)
    {
        _playbackConfig = playbackConfig;
        _logger = logger;

        _lastPlaybackConfig = _playbackConfig.CurrentValue;
        playbackConfig.OnChange(OnPlaybackConfigChanged); // triggers AllNotesOff if transpose changes
    }

    public void RegisterHandler(INoteEventHandler noteEventHandler)
    {
        _noteEventHandlers.Add(noteEventHandler);
    }

    public void NoteOn(NotePitch pitch)
    {
        NoteVelocity velocity = _playbackConfig.CurrentValue.Velocity;

        if (velocity.Value == 0) // MIDI spec: velocity 0 = note-off
        {
            NoteOff(pitch);
            return;
        }

        NotePitch transposedPitch = TransposePitch(pitch);

        if (_activeNotes.TryAdd(transposedPitch, 1))
        {
            NoteOnEvent noteOnEvent = new(transposedPitch, velocity);

            _logger.LogInformation("Playing note {Pitch}", transposedPitch);

            foreach (INoteEventHandler noteEventHandler in _noteEventHandlers)
            {
                noteEventHandler.OnNoteOn(noteOnEvent);
            }
        }
        else // note already active
        {
            _activeNotes[transposedPitch]++;
        }
    }

    public void NoteOff(NotePitch pitch)
    {
        NotePitch transposedPitch = TransposePitch(pitch);

        if (_activeNotes.TryGetValue(transposedPitch, out int activeCount))
        {
            if (activeCount == 1)
            {
                NoteOffEvent noteOffEvent = new(transposedPitch);

                _logger.LogInformation("Releasing note {Pitch}", transposedPitch);

                foreach (INoteEventHandler noteEventHandler in _noteEventHandlers)
                {
                    noteEventHandler.OnNoteOff(noteOffEvent);
                }

                _activeNotes.Remove(transposedPitch); // notes with 0 active play are removed from dictionary
            }
            else // activeCount > 1
            {
                _activeNotes[transposedPitch] = activeCount - 1;
            }
        }
    }

    public void AllNotesOff()
    {
        foreach ((NotePitch transposedPitch, int _) in _activeNotes)
        {
            NoteOffEvent noteOffEvent = new(transposedPitch);

            foreach (INoteEventHandler noteEventHandler in _noteEventHandlers)
            {
                noteEventHandler.OnNoteOff(noteOffEvent);
            }
        }

        _activeNotes.Clear();
    }

    private void OnPlaybackConfigChanged(PlaybackConfig newConfig)
    {
        if (newConfig.Transpose != _lastPlaybackConfig.Transpose)
        {
            AllNotesOff();
        }
        _lastPlaybackConfig = newConfig;
    }

    private NotePitch TransposePitch(NotePitch pitch)
    {
        short transpose = _playbackConfig.CurrentValue.Transpose;

        return new NotePitch((ushort)Math.Clamp(pitch.Value + transpose, NotePitch.MinValue, NotePitch.MaxValue));
    }
}
