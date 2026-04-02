namespace Klavier.Audio.Options;

public class AudioConfig
{
    public string SoundFontPath { get; init; } = "TODO";
    public string AudioDriver { get; init; } = "dsound";
    public ushort Volume { get; init; } = 75;
}
