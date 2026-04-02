using Klavier.Core.Ports;

namespace Klavier.Core.Engine;

public interface IPianoEngine
{
    void RegisterHandler(INoteEventHandler noteEventHandler);
    public void NoteOn(ushort pitch);
    public void NoteOff(ushort pitch);
    void AllNotesOff();
}
