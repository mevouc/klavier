using Klavier.Audio.Options;
using Klavier.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Klavier.Audio;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluidSynthAudio(
        this IServiceCollection services,
        IConfigurationSection audioSection)
    {
        services.Configure<AudioConfig>(audioSection);
        services.AddSingleton<IAudioOutput, FluidSynthAudioOutput>();

        return services;
    }
}
