using System.Collections.Concurrent;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class DefaultSourceSchemaClientScope : ISourceSchemaClientScope
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly ConcurrentDictionary<(string Name, OperationType Type), ISourceSchemaClient> _clients = [];
    private ClientEntry? _firstClient;
    private readonly ISourceSchemaClientFactory[] _clientFactories;
    private readonly FusionSchemaDefinition _schemaDefinition;
    private readonly SourceSchemaClientConfigurations _configurations;
    private bool _disposed;

    public DefaultSourceSchemaClientScope(
        FusionSchemaDefinition schemaDefinition,
        ISourceSchemaClientFactory[] clientFactories)
    {
        ArgumentNullException.ThrowIfNull(schemaDefinition);
        ArgumentNullException.ThrowIfNull(clientFactories);

        _clientFactories = clientFactories;
        _schemaDefinition = schemaDefinition;
        _configurations = schemaDefinition.Features.GetRequired<SourceSchemaClientConfigurations>();
    }

    public ISourceSchemaClient GetClient(string name, OperationType operationType)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var key = (name, operationType);
        var firstClient = Volatile.Read(ref _firstClient);

        if (firstClient is not null && firstClient.Key == key)
        {
            return firstClient.Client;
        }

        if (!_clients.TryGetValue(key, out var sourceSchemaClient))
        {
            lock (_sync)
            {
                firstClient = Volatile.Read(ref _firstClient);

                if (firstClient is not null && firstClient.Key == key)
                {
                    return firstClient.Client;
                }

                if (!_clients.TryGetValue(key, out sourceSchemaClient))
                {
                    if (!_configurations.TryGet(name, operationType, out var config))
                    {
                        throw new InvalidOperationException(
                            $"No client configuration found for schema '{name}' and operation type {operationType}.");
                    }

                    sourceSchemaClient = CreateClient(config);

                    firstClient = Volatile.Read(ref _firstClient);

                    if (firstClient is null)
                    {
                        Volatile.Write(ref _firstClient, new ClientEntry(key, sourceSchemaClient));
                    }
                    else if (firstClient.Key != key)
                    {
                        _clients.TryAdd(key, sourceSchemaClient);
                    }
                }
            }
        }

        return sourceSchemaClient;
    }

    private ISourceSchemaClient CreateClient(ISourceSchemaClientConfiguration configuration)
    {
        var factories = _clientFactories;
        var schema = _schemaDefinition;

        if (factories.Length > 0 && factories[0].CanHandle(configuration))
        {
            return factories[0].CreateClient(schema, configuration);
        }

        if (factories.Length > 1 && factories[1].CanHandle(configuration))
        {
            return factories[1].CreateClient(schema, configuration);
        }

        if (factories.Length > 2 && factories[2].CanHandle(configuration))
        {
            return factories[2].CreateClient(schema, configuration);
        }

        if (factories.Length > 3 && factories[3].CanHandle(configuration))
        {
            return factories[3].CreateClient(schema, configuration);
        }

        if (factories.Length > 4)
        {
            for (var i = 4; i < factories.Length; i++)
            {
                if (factories[i].CanHandle(configuration))
                {
                    return factories[i].CreateClient(schema, configuration);
                }
            }
        }

        throw new NotSupportedException(
            $"No client factory found for configuration type: {configuration.GetType().Name}.");
    }

    /// <summary>
    /// Disposes the source schema clients created during the current request and clears the
    /// scope so the container can be reused for the next request.
    /// </summary>
    internal async ValueTask ResetAsync()
    {
        if (_disposed)
        {
            return;
        }

        var firstClient = Volatile.Read(ref _firstClient);
        var clients = _clients.IsEmpty ? null : _clients.Values;

        if (firstClient is null && clients is null)
        {
            return;
        }

        if (firstClient is not null)
        {
            await firstClient.Client.DisposeAsync().ConfigureAwait(false);
        }

        if (clients is not null)
        {
            foreach (var client in clients)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }

        _clients.Clear();
        Volatile.Write(ref _firstClient, null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await ResetAsync().ConfigureAwait(false);
        _disposed = true;
    }

    private sealed class ClientEntry(
        (string Name, OperationType Type) key,
        ISourceSchemaClient client)
    {
        public (string Name, OperationType Type) Key { get; } = key;

        public ISourceSchemaClient Client { get; } = client;
    }
}
