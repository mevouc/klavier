namespace Klavier.Core.Ports;

public interface IAudioOutput : INoteEventHandler, IDisposable
{
    void Initialize();
}
