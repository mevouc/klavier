using Klavier.Core.Events;
using Klavier.Core.Options;
using Klavier.Core.Ports;
using Microsoft.Extensions.Options;

namespace Klavier.Core.Engine;

public class PianoEngine : IPianoEngine
{
    private const int _MidiPitchMin = 0;
    private const int _MidiPitchMax = 127;

    private readonly IOptionsMonitor<PlaybackConfig> _playbackConfig;
    private readonly Dictionary<ushort, int> _activeNotes = []; // value is active inputs count (note plays when there's at least one)
    private readonly HashSet<INoteEventHandler> _noteEventHandlers = [];
    private short _lastTranspose;

    public PianoEngine(IOptionsMonitor<PlaybackConfig> playbackConfig)
    {
        _playbackConfig = playbackConfig;

        _lastTranspose = _playbackConfig.CurrentValue.Transpose;
        playbackConfig.OnChange(OnPlaybackConfigChanged); // triggers AllNotesOff if transpose changes
    }

    public void RegisterHandler(INoteEventHandler noteEventHandler)
    {
        _noteEventHandlers.Add(noteEventHandler);
    }

    public void NoteOn(ushort pitch)
    {
        ushort transposedPitch = TransposePitch(pitch);

        if (_activeNotes.TryGetValue(transposedPitch, out int activeCount) && activeCount > 0)
        {
            _activeNotes[transposedPitch] = activeCount + 1;
        }
        else
        {
            _activeNotes[transposedPitch] = 1;

            NoteOnEvent noteOnEvent = new(transposedPitch, _playbackConfig.CurrentValue.Velocity);

            foreach (INoteEventHandler noteEventHandler in _noteEventHandlers)
            {
                noteEventHandler.OnNoteOn(noteOnEvent);
            }
        }
    }

    public void NoteOff(ushort pitch)
    {
        ushort transposedPitch = TransposePitch(pitch);

        if (_activeNotes.TryGetValue(transposedPitch, out int activeCount))
        {
            if (activeCount == 1)
            {
                NoteOffEvent noteOffEvent = new(transposedPitch);

                foreach (INoteEventHandler noteEventHandler in _noteEventHandlers)
                {
                    noteEventHandler.OnNoteOff(noteOffEvent);
                }

                _activeNotes.Remove(transposedPitch);
            }
            else // activeCount > 1
            {
                _activeNotes[transposedPitch] = activeCount - 1;
            }
        }
    }

    public void AllNotesOff()
    {
        foreach ((ushort transposedPitch, int _) in _activeNotes)
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
        if (newConfig.Transpose != _lastTranspose)
        {
            _lastTranspose = newConfig.Transpose;
            AllNotesOff();
        }
    }

    private ushort TransposePitch(ushort pitch)
    {
        short transpose = _playbackConfig.CurrentValue.Transpose;

        return (ushort)Math.Clamp(pitch + transpose, _MidiPitchMin, _MidiPitchMax);
    }
}
