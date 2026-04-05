using Klavier.Core.Ports;
using Klavier.Core.Primitives;

namespace Klavier.Core.Engine;

public interface IPianoEngine
{
    void RegisterHandler(INoteEventHandler noteEventHandler);
    void NoteOn(NotePitch pitch);
    void NoteOff(NotePitch pitch);
    void AllNotesOff();
}
