using Klavier.Core.Primitives;

namespace Klavier.Core.Options;

public class PlaybackConfig
{
    public NoteVelocity Velocity { get; init; } = new(100);
    public short Transpose { get; init; } = 0;
}
