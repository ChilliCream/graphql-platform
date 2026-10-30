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
    private readonly ConcurrentDictionary<(string Name, OperationType Type), ClientEntry> _clients = [];
    private readonly ISourceSchemaClientFactory[] _clientFactories;
    private readonly FusionSchemaDefinition _schemaDefinition;
    private readonly SourceSchemaClientConfigurations _configurations;
    private long _requestTimestamp;
    private int _ownedClientCount;
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

        if (_clients.TryGetValue(key, out var entry) && entry.IsValid(_requestTimestamp))
        {
            return entry.Client;
        }

        lock (_sync)
        {
            if (!_clients.TryGetValue(key, out entry) || !entry.IsValid(_requestTimestamp))
            {
                if (!_configurations.TryGet(name, operationType, out var config))
                {
                    throw ThrowHelper.ClientConfigurationNotFound(name, operationType);
                }

                // A shared client that is no longer valid is replaced, not disposed.
                entry = CreateClient(config);
                _clients[key] = entry;

                if (entry.SharedClient is null)
                {
                    _ownedClientCount++;
                }
            }
        }

        return entry.Client;
    }

    /// <summary>
    /// Starts a request that takes shared clients which are valid at the given
    /// <see cref="System.Diagnostics.Stopwatch"/> timestamp.
    /// </summary>
    internal void BeginRequest(long timestamp) => _requestTimestamp = timestamp;

    private ClientEntry CreateClient(ISourceSchemaClientConfiguration configuration)
    {
        var factory = GetFactory(configuration);

        if (factory is ISharedSourceSchemaClientFactory sharedFactory
            && sharedFactory.TryGetSharedClient(_schemaDefinition, configuration, out var sharedClient))
        {
            return new ClientEntry(sharedClient.Client, sharedClient);
        }

        return new ClientEntry(factory.CreateClient(_schemaDefinition, configuration), SharedClient: null);
    }

    private ISourceSchemaClientFactory GetFactory(ISourceSchemaClientConfiguration configuration)
    {
        var factories = _clientFactories;

        if (factories.Length > 0 && factories[0].CanHandle(configuration))
        {
            return factories[0];
        }

        if (factories.Length > 1 && factories[1].CanHandle(configuration))
        {
            return factories[1];
        }

        if (factories.Length > 2 && factories[2].CanHandle(configuration))
        {
            return factories[2];
        }

        if (factories.Length > 3 && factories[3].CanHandle(configuration))
        {
            return factories[3];
        }

        if (factories.Length > 4)
        {
            for (var i = 4; i < factories.Length; i++)
            {
                if (factories[i].CanHandle(configuration))
                {
                    return factories[i];
                }
            }
        }

        throw new NotSupportedException(
            $"No client factory found for configuration type: {configuration.GetType().Name}.");
    }

    /// <summary>
    /// Ends the current request so the scope can serve the next one. Shared clients are kept;
    /// all other clients are disposed.
    /// </summary>
    internal ValueTask ResetAsync()
    {
        if (_disposed || _ownedClientCount == 0)
        {
            return ValueTask.CompletedTask;
        }

        return DisposeOwnedClientsAsync();
    }

    private async ValueTask DisposeOwnedClientsAsync()
    {
        foreach (var (key, entry) in _clients)
        {
            // Shared clients may still be in use by other requests, so they are never disposed.
            if (entry.SharedClient is null)
            {
                _clients.TryRemove(key, out _);
                await entry.Client.DisposeAsync().ConfigureAwait(false);
            }
        }

        _ownedClientCount = 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DisposeOwnedClientsAsync().ConfigureAwait(false);
        _clients.Clear();
    }

    private readonly record struct ClientEntry(ISourceSchemaClient Client, SharedSourceSchemaClient? SharedClient)
    {
        public bool IsValid(long requestTimestamp)
            => SharedClient is null || SharedClient.IsValid(requestTimestamp);
    }
}
