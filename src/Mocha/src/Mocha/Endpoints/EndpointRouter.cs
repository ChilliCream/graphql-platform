using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Mocha;

/// <summary>
/// Routes dispatch endpoints by address using a thread-safe, multi-index lookup. Supports resolution, creation, and aliasing of endpoints across all configured transports.
/// </summary>
public sealed class EndpointRouter : IEndpointRouter
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    // Primary storage - endpoint -> tracked addresses
    private readonly Dictionary<DispatchEndpoint, ImmutableHashSet<Uri>> _endpoints = [];

    // Single unified index - all addresses (endpoint address, resource address, aliases) map to endpoints
    private readonly Dictionary<Uri, ImmutableHashSet<DispatchEndpoint>> _byAddress = [];

    /// <inheritdoc />
    public IReadOnlyList<DispatchEndpoint> Endpoints
    {
        get
        {
            lock (_lock)
            {
                return [.. _endpoints.Keys];
            }
        }
    }

    /// <inheritdoc />
    public bool TryGet(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        ArgumentNullException.ThrowIfNull(address);

        lock (_lock)
        {
            if (_byAddress.TryGetValue(address, out var endpoints) && !endpoints.IsEmpty)
            {
                endpoint = endpoints.First();
                return true;
            }

            endpoint = null;
            return false;
        }
    }

    /// <inheritdoc />
    public ImmutableHashSet<DispatchEndpoint> GetAll(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        lock (_lock)
        {
            return _byAddress.TryGetValue(address, out var set) ? set : [];
        }
    }

    /// <inheritdoc />
    public DispatchEndpoint GetOrCreate(IMessagingConfigurationContext context, Uri address)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(address);

        // First try fast read path
        if (TryGet(address, out var existing))
        {
            return existing;
        }

        // Need write lock for resolution/creation
        lock (_lock)
        {
            // Double-check after acquiring write lock
            if (_byAddress.TryGetValue(address, out var endpoints) && !endpoints.IsEmpty)
            {
                return endpoints.First();
            }

            // Ask each transport if they already have this endpoint
            foreach (var transport in context.Transports)
            {
                if (transport.TryGetDispatchEndpoint(address, out var endpoint))
                {
                    AddOrUpdateInternal(endpoint, address);
                    return endpoint;
                }
            }

            // Try to create on each transport
            foreach (var transport in context.Transports)
            {
                var configuration = transport.CreateEndpointConfiguration(context, address);
                if (configuration is not null)
                {
                    var endpoint = transport.AddEndpoint(context, configuration);

                    endpoint.DiscoverTopology(context);

                    endpoint.Complete(context);

                    AddOrUpdateInternal(endpoint, address);
                    return endpoint;
                }
            }

            throw new InvalidOperationException($"No transport can handle address: {address}");
        }
    }

    /// <inheritdoc />
    public void AddOrUpdate(DispatchEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        lock (_lock)
        {
            AddOrUpdateInternal(endpoint, null);
        }
    }

    /// <inheritdoc />
    public void AddAddress(DispatchEndpoint endpoint, Uri address)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(address);

        lock (_lock)
        {
            if (!_endpoints.TryGetValue(endpoint, out var addresses))
            {
                throw new InvalidOperationException("Endpoint must be registered before adding addresses");
            }

            if (addresses.Contains(address))
            {
                return; // Already has this address
            }

            _endpoints[endpoint] = addresses.Add(address);
            AddToIndex(_byAddress, address, endpoint);
        }
    }

    /// <inheritdoc />
    public void Remove(DispatchEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        lock (_lock)
        {
            if (!_endpoints.TryGetValue(endpoint, out var addresses))
            {
                return;
            }

            // Remove from all address indexes
            foreach (var address in addresses)
            {
                RemoveFromIndex(_byAddress, address, endpoint);
            }

            _endpoints.Remove(endpoint);
        }
    }

    private void AddOrUpdateInternal(DispatchEndpoint endpoint, Uri? resolvedAddress)
    {
        var endpointAddress = endpoint.Address;
        var resourceAddress = endpoint.Destination?.Address;

        if (_endpoints.TryGetValue(endpoint, out var oldAddresses))
        {
            // Build new set of addresses
            var newAddresses = ImmutableHashSet<Uri>.Empty;

            if (endpointAddress is not null)
            {
                newAddresses = newAddresses.Add(endpointAddress);
            }

            if (resourceAddress is not null)
            {
                newAddresses = newAddresses.Add(resourceAddress);
            }

            if (resolvedAddress is not null)
            {
                newAddresses = newAddresses.Add(resolvedAddress);
            }

            // Preserve any additional addresses that were manually added
            foreach (var addr in oldAddresses)
            {
                if (addr != endpointAddress && addr != resourceAddress)
                {
                    newAddresses = newAddresses.Add(addr);
                }
            }

            // Remove addresses that are no longer valid
            foreach (var addr in oldAddresses.Except(newAddresses))
            {
                RemoveFromIndex(_byAddress, addr, endpoint);
            }

            // Add new addresses
            foreach (var addr in newAddresses.Except(oldAddresses))
            {
                AddToIndex(_byAddress, addr, endpoint);
            }

            _endpoints[endpoint] = newAddresses;
        }
        else
        {
            // New endpoint
            var addresses = ImmutableHashSet<Uri>.Empty;

            if (endpointAddress is not null)
            {
                addresses = addresses.Add(endpointAddress);
                AddToIndex(_byAddress, endpointAddress, endpoint);
            }

            if (resourceAddress is not null)
            {
                addresses = addresses.Add(resourceAddress);
                AddToIndex(_byAddress, resourceAddress, endpoint);
            }

            if (resolvedAddress is not null && !addresses.Contains(resolvedAddress))
            {
                addresses = addresses.Add(resolvedAddress);
                AddToIndex(_byAddress, resolvedAddress, endpoint);
            }

            _endpoints[endpoint] = addresses;
        }
    }

    private static void AddToIndex(
        Dictionary<Uri, ImmutableHashSet<DispatchEndpoint>> dict,
        Uri key,
        DispatchEndpoint value)
    {
        if (dict.TryGetValue(key, out var set))
        {
            dict[key] = set.Add(value);
        }
        else
        {
            dict[key] = [value];
        }
    }

    private static void RemoveFromIndex(
        Dictionary<Uri, ImmutableHashSet<DispatchEndpoint>> dict,
        Uri key,
        DispatchEndpoint value)
    {
        if (dict.TryGetValue(key, out var set))
        {
            set = set.Remove(value);
            if (set.IsEmpty)
            {
                dict.Remove(key);
            }
            else
            {
                dict[key] = set;
            }
        }
    }
}
