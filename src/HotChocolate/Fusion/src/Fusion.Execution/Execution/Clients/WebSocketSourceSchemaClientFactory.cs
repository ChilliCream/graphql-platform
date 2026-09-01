using System.Collections.Concurrent;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class WebSocketSourceSchemaClientFactory
    : SourceSchemaClientFactory<WebSocketSourceSchemaClientConfiguration>
    , IDisposable
{
    private readonly ConcurrentDictionary<string, HttpMessageInvoker> _invokers =
        new(StringComparer.Ordinal);
    private bool _disposed;

    protected override ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        WebSocketSourceSchemaClientConfiguration configuration)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var invoker = _invokers.GetOrAdd(
            configuration.Name,
            static _ => new HttpMessageInvoker(
                new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true
                }));

        return new WebSocketSourceSchemaClient(invoker, configuration);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var invoker in _invokers.Values)
        {
            invoker.Dispose();
        }

        _invokers.Clear();
        _disposed = true;
    }
}
