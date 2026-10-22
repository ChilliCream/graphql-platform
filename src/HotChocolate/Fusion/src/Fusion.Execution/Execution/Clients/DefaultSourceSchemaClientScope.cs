using System.Collections.Concurrent;
using System.Diagnostics;
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
    private long _nextExpiration = long.MaxValue;
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

        if (_clients.TryGetValue(key, out var entry))
        {
            return entry.Client;
        }

        lock (_sync)
        {
            if (!_clients.TryGetValue(key, out entry))
            {
                if (!_configurations.TryGet(name, operationType, out var config))
                {
                    throw ThrowHelper.ClientConfigurationNotFound(name, operationType);
                }

                entry = CreateClient(config);
                _clients.TryAdd(key, entry);

                if (entry.ExpiresAt < _nextExpiration)
                {
                    _nextExpiration = entry.ExpiresAt;
                }
            }
        }

        return entry.Client;
    }

    private ClientEntry CreateClient(ISourceSchemaClientConfiguration configuration)
    {
        var factory = GetFactory(configuration);

        if (factory is ISharedSourceSchemaClientFactory sharedFactory
            && sharedFactory.TryGetSharedClient(_schemaDefinition, configuration, out var client, out var expiresAt))
        {
            return new ClientEntry(client, expiresAt, IsShared: true);
        }

        return new ClientEntry(factory.CreateClient(_schemaDefinition, configuration), ExpiresAt: 0, IsShared: false);
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
    /// Ends the current request so the scope can serve the next one. Shared clients are kept
    /// until they expire; all other clients are disposed.
    /// </summary>
    internal ValueTask ResetAsync()
    {
        var now = Stopwatch.GetTimestamp();

        if (_disposed || now < _nextExpiration)
        {
            return ValueTask.CompletedTask;
        }

        return ReleaseClientsAsync(now);
    }

    private async ValueTask ReleaseClientsAsync(long now)
    {
        var nextExpiration = long.MaxValue;

        foreach (var (key, entry) in _clients)
        {
            if (entry.ExpiresAt <= now)
            {
                _clients.TryRemove(key, out _);

                // Shared clients may still be in use by other requests, so they are only released.
                if (!entry.IsShared)
                {
                    await entry.Client.DisposeAsync().ConfigureAwait(false);
                }
            }
            else if (entry.ExpiresAt < nextExpiration)
            {
                nextExpiration = entry.ExpiresAt;
            }
        }

        _nextExpiration = nextExpiration;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await ReleaseClientsAsync(long.MaxValue).ConfigureAwait(false);
    }

    private readonly record struct ClientEntry(ISourceSchemaClient Client, long ExpiresAt, bool IsShared);
}
