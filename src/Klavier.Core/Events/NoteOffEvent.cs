namespace Klavier.Core.Events;

public readonly record struct NoteOffEvent(
    ushort Pitch);
