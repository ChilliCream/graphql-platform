using static Microsoft.Extensions.DependencyInjection.GatewayConfigurationFileUtils;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class StaticGatewayConfigurationFileObserver : IObservable<GatewayConfiguration>
{
    private readonly string _filename;

    public StaticGatewayConfigurationFileObserver(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(filename));
        }

        _filename = filename;
    }

    public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
    {
        return new FileConfigurationSession(observer, _filename);
    }

    private class FileConfigurationSession : IDisposable
    {
        public FileConfigurationSession(IObserver<GatewayConfiguration> observer, string filename)
        {
            Task.Run(
                async () =>
                {
                    try
                    {
                        var document = await LoadDocumentAsync(filename, default);
                        observer.OnNext(new GatewayConfiguration(document));
                    }
                    catch(Exception ex)
                    {
                        observer.OnError(ex);
                        observer.OnCompleted();
                    }
                });
        }

        public void Dispose()
        {
            // there is nothing to dispose here.
        }
    }
}
