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
    private List<ISourceSchemaClient>? _retiredClients;
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
                    throw new InvalidOperationException(
                        $"No client configuration found for schema '{name}' and operation type {operationType}.");
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
        var client = factory.CreateClient(_schemaDefinition, configuration);
        var expiresAt = 0L;

        if (factory is IReusableSourceSchemaClientFactory reusableFactory)
        {
            var maxAge = reusableFactory.GetMaxClientAge(configuration);

            if (maxAge > TimeSpan.Zero)
            {
                expiresAt = Stopwatch.GetTimestamp() + (long)(maxAge.TotalSeconds * Stopwatch.Frequency);
            }
        }

        return new ClientEntry(client, expiresAt);
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
    /// Removes the clients that reached their maximum age so that the next request creates new ones.
    /// The removed clients are disposed by the next <see cref="ResetAsync"/>.
    /// </summary>
    internal void RetireExpiredClients()
    {
        if (_clients.IsEmpty)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();

        if (now < _nextExpiration)
        {
            return;
        }

        var nextExpiration = long.MaxValue;

        foreach (var (key, entry) in _clients)
        {
            if (entry.ExpiresAt <= now)
            {
                _clients.TryRemove(key, out _);
                (_retiredClients ??= []).Add(entry.Client);
            }
            else if (entry.ExpiresAt < nextExpiration)
            {
                nextExpiration = entry.ExpiresAt;
            }
        }

        _nextExpiration = nextExpiration;
    }

    /// <summary>
    /// Ends the current request so the scope can serve the next one. Clients of reusable
    /// factories are kept until they reach their maximum age; all other clients are disposed.
    /// </summary>
    internal ValueTask ResetAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        var now = Stopwatch.GetTimestamp();

        if (now < _nextExpiration && _retiredClients is not { Count: > 0 })
        {
            return ValueTask.CompletedTask;
        }

        return DisposeExpiredClientsAsync(now);
    }

    private async ValueTask DisposeExpiredClientsAsync(long now)
    {
        if (_retiredClients is { Count: > 0 } retiredClients)
        {
            foreach (var client in retiredClients)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }

            retiredClients.Clear();
        }

        var nextExpiration = long.MaxValue;

        foreach (var (key, entry) in _clients)
        {
            if (entry.ExpiresAt <= now)
            {
                _clients.TryRemove(key, out _);
                await entry.Client.DisposeAsync().ConfigureAwait(false);
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
        await DisposeExpiredClientsAsync(long.MaxValue).ConfigureAwait(false);
    }

    private readonly record struct ClientEntry(ISourceSchemaClient Client, long ExpiresAt);
}
