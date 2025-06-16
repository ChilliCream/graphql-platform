using System.Collections.Concurrent;
using HotChocolate.Caching.Memory;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class DefaultSourceSchemaClientScope : ISourceSchemaClientScope
{
    private readonly FusionSchemaDefinition _schemaDefinition;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<(string Name, OperationType Type), ISourceSchemaClient> _clients = [];
    private readonly Cache<string> _operationStringCache;
    private bool _disposed;

    public DefaultSourceSchemaClientScope(
        FusionSchemaDefinition schemaDefinition,
        IHttpClientFactory httpClientFactory,
        Cache<string> operationStringCache)
    {
        ArgumentNullException.ThrowIfNull(schemaDefinition);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(operationStringCache);

        _schemaDefinition = schemaDefinition;
        _httpClientFactory = httpClientFactory;
        _operationStringCache = operationStringCache;
    }

    public ISourceSchemaClient GetClient(string name, OperationType type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _clients.GetOrAdd(
            (name, type),
            static (key, state) =>
            {
                var httpClient = state._httpClientFactory.CreateClient(key.Name);
                var graphqlHttpClient = GraphQLHttpClient.Create(httpClient, disposeHttpClient: true);
                return new SourceSchemaHttpClient(graphqlHttpClient, state._operationStringCache);
            },
            (_httpClientFactory, _operationStringCache));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }

        _clients.Clear();
        _disposed = true;
    }
}
