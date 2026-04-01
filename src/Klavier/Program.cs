using MeltySynth;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using Klavier;

using FileStream sfFile = new("C:\\Users\\mevouc\\Desktop\\GRAND PIANO.sf2", FileMode.Open, FileAccess.Read, FileShare.Read);
SoundFont soundFont = new(sfFile);
Synthesizer synthesizer = new(soundFont, 44100);
MeltySynthProvider provider = new(synthesizer);

using WasapiOut outputDevice = new(AudioClientShareMode.Shared, 50);

outputDevice.Init(provider);
Console.WriteLine("Playing Middle C...");
outputDevice.Play();

synthesizer.NoteOn(0, 60, 100);
Thread.Sleep(2000);

Console.WriteLine("Releasing...");
synthesizer.NoteOff(0, 60);
Thread.Sleep(1000);

outputDevice.Stop();
outputDevice.Dispose();
Console.WriteLine("Stop, dispose, exit. Audio device released...");

Environment.Exit(0);

namespace Klavier
{
    public class MeltySynthProvider(Synthesizer synth) : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(synth.SampleRate, 2);

        public int Read(float[] buffer, int offset, int count)
        {
            synth.RenderInterleaved(buffer.AsSpan(offset, count));
            return count;
        }
    }
}