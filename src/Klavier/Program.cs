using Avalonia;
using Klavier.Audio;
using Klavier.Core.Engine;
using Klavier.Core.Options;
using Klavier.Core.Ports;
using Klavier.UI;
using Klavier.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((context, services) =>
    {
        IConfiguration configuration = context.Configuration;

        services.Configure<PlaybackConfig>(configuration.GetSection("Playback"));
        services.AddFluidSynthAudio(configuration.GetSection("Audio"));
        services.AddSingleton<IPianoEngine, PianoEngine>();
        services.AddTransient<MainWindow>();
    })
    .Build();

// Initialize audio and register it as a note event handler
IAudioOutput audio = host.Services.GetRequiredService<IAudioOutput>();
audio.Initialize();

IPianoEngine engine = host.Services.GetRequiredService<IPianoEngine>();
engine.RegisterHandler(audio);

AppBuilder.Configure(() => new App(() => host.Services.GetRequiredService<MainWindow>()))
    .UsePlatformDetect()
    .StartWithClassicDesktopLifetime(args);
