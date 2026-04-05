using Klavier.Core.Primitives;

namespace Klavier.Core.Events;

public readonly record struct NoteOffEvent(
    NotePitch Pitch);
