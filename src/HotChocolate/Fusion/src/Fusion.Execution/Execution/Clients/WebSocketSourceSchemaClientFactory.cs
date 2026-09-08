using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class WebSocketSourceSchemaClientFactory
    : SourceSchemaClientFactory<WebSocketSourceSchemaClientConfiguration>
    , IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, HttpMessageInvoker> _invokers = new(StringComparer.Ordinal);
    private readonly Func<HttpMessageInvoker> _createInvoker;
    private bool _disposed;

    public WebSocketSourceSchemaClientFactory()
        : this(CreateInvoker)
    {
    }

    internal WebSocketSourceSchemaClientFactory(Func<HttpMessageInvoker> createInvoker)
    {
        ArgumentNullException.ThrowIfNull(createInvoker);
        _createInvoker = createInvoker;
    }

    protected override ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        WebSocketSourceSchemaClientConfiguration configuration)
    {
        HttpMessageInvoker invoker;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_invokers.TryGetValue(configuration.Name, out invoker!))
            {
                invoker = _createInvoker();
                _invokers.Add(configuration.Name, invoker);
            }
        }

        return new WebSocketSourceSchemaClient(invoker, configuration);
    }

    public void Dispose()
    {
        HttpMessageInvoker[] invokers;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            invokers = [.. _invokers.Values];
            _invokers.Clear();
        }

        foreach (var invoker in invokers)
        {
            invoker.Dispose();
        }
    }

    private static HttpMessageInvoker CreateInvoker()
        => new(
            new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            });
}
