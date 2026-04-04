using Klavier.Audio;
using Klavier.Core.Engine;
using Klavier.Core.Options;
using Klavier.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((context, services) =>
    {
        IConfiguration configuration = context.Configuration;

        services.Configure<PlaybackConfig>(configuration.GetSection("Playback"));
        services.AddFluidSynthAudio(configuration.GetSection("Audio"));
        services.AddSingleton<IPianoEngine, PianoEngine>();
    })
    .Build();

ILogger logger = host.Services.GetRequiredService<ILogger<Program>>();

// POC
IPianoEngine engine = host.Services.GetRequiredService<IPianoEngine>();
IAudioOutput audio = host.Services.GetRequiredService<IAudioOutput>();

audio.Initialize();
engine.RegisterHandler(audio);

logger.LogInformation("Playing Middle C...");
engine.NoteOn(60);
Thread.Sleep(2000);

logger.LogInformation("Releasing...");
engine.NoteOff(60);
Thread.Sleep(1000);

logger.LogInformation("Audio device released.");
// POC ends
