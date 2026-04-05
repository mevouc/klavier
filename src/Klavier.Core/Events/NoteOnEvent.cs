using Klavier.Core.Primitives;

namespace Klavier.Core.Events;

public readonly record struct NoteOnEvent(
    NotePitch Pitch,
    NoteVelocity Velocity);
