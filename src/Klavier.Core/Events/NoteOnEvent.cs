namespace Klavier.Core.Events;

public readonly record struct NoteOnEvent(
    ushort Pitch,
    ushort Velocity);
