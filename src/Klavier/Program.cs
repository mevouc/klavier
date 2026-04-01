using NFluidsynth;

Logger.SetLoggerMethod((level, message, _) =>
{
    if (level <= Logger.LogLevel.Error)
    {
        Console.Error.WriteLine($"FluidSynth ({level}): {message}");
    }
});

using Settings settings = new();
settings[ConfigurationKeys.AudioDriver].StringValue = "dsound";

using Synth synth = new(settings);
synth.LoadSoundFont("C:\\Users\\MevenCourouble\\Desktop\\GRAND PIANO.sf2", true);

using AudioDriver driver = new(settings, synth);

Console.WriteLine("Playing Middle C...");
synth.NoteOn(0, 60, 100);
Thread.Sleep(2000);

Console.WriteLine("Releasing...");
synth.NoteOff(0, 60);
Thread.Sleep(1000);

Console.WriteLine("Audio device released.");
