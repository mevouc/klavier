using NFluidsynth;

namespace Klavier.Audio.Options;

public class AudioConfig
{
    public string SoundFontPath { get; init; } = "C:\\Users\\mevouc\\Desktop\\GRAND PIANO.sf2";
    public string AudioDriver { get; init; } = "dsound";
    public string MinimumFluidSynthLogLevel { get; init; } = "Error";
    public Logger.LogLevel FluidSynthLogLevel => Enum.Parse<Logger.LogLevel>(MinimumFluidSynthLogLevel);
    public ushort VolumeInPercent { get; init; } = 60;
    public float GainFactor => VolumeInPercent / 100f;
}
