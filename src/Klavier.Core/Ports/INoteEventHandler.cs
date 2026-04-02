using Klavier.Core.Events;

namespace Klavier.Core.Ports;

public interface INoteEventHandler
{
    void OnNoteOn(NoteOnEvent noteOnEvent);
    void OnNoteOff(NoteOffEvent noteOffEvent);
}
