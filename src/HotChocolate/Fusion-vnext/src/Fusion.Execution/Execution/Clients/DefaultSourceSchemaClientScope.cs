using System.Collections.Concurrent;
using HotChocolate.Caching.Memory;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class DefaultSourceSchemaClientScope : ISourceSchemaClientScope
{
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<(string Name, OperationType Type), ISourceSchemaClient> _clients = [];
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SourceSchemaClientConfigurations _configurations;
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

        _httpClientFactory = httpClientFactory;
        _configurations = schemaDefinition.Features.GetRequired<SourceSchemaClientConfigurations>();
        _operationStringCache = operationStringCache;
    }

    public ISourceSchemaClient GetClient(string name, OperationType operationType)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var key = (name, operationType);

        if (!_clients.TryGetValue(key, out var sourceSchemaClient))
        {
            lock (_sync)
            {
                if (!_clients.TryGetValue(key, out sourceSchemaClient))
                {
                    if (!_configurations.TryGet(name, operationType, out var config))
                    {
                        throw new InvalidOperationException(
                            $"No client configuration found for schema {name} and operation type {operationType}.");
                    }

                    switch (config)
                    {
                        case SourceSchemaHttpClientConfiguration httpClientConfig:
                            var httpClient = _httpClientFactory.CreateClient(httpClientConfig.HttpClientName);
                            httpClient.BaseAddress = httpClientConfig.BaseAddress;

                            sourceSchemaClient = new SourceSchemaHttpClient(
                                GraphQLHttpClient.Create(httpClient, disposeHttpClient: true),
                                httpClientConfig,
                                _operationStringCache);

                            _clients.TryAdd(key, sourceSchemaClient);
                            break;

                        default:
                            throw new NotSupportedException(
                                $"Unsupported client configuration type: {config.GetType().Name}.");
                    }
                }
            }
        }

        return sourceSchemaClient;
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
