using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class StaticGatewayConfigurationObserver : IObservable<GatewayConfiguration>
{
    private readonly DocumentNode _gatewayConfigDoc;

    public StaticGatewayConfigurationObserver(DocumentNode gatewayConfigDoc)
    {
        _gatewayConfigDoc = gatewayConfigDoc ??
            throw new ArgumentNullException(nameof(gatewayConfigDoc));
    }

    public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
    {
        return new FileConfigurationSession(observer, _gatewayConfigDoc);
    }

    private sealed class FileConfigurationSession : IDisposable
    {
        public FileConfigurationSession(
            IObserver<GatewayConfiguration> observer,
            DocumentNode gatewayConfigDoc)
        {
            observer.OnNext(new(gatewayConfigDoc));
        }

        public void Dispose()
        {
            // there is nothing to dispose here.
        }
    }
}
