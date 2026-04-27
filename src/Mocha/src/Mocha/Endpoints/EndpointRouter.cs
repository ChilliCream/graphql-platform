using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

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

    private readonly ChangeTokenSource _changeTokens = new();

    /// <inheritdoc />
    public IChangeToken GetChangeToken() => _changeTokens.Current;

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
            if (TryGetEndpoint(address, out endpoint))
            {
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
            if (!_byAddress.TryGetValue(address, out var endpoints))
            {
                return [];
            }

            return endpoints;
        }
    }

    /// <inheritdoc />
    public DispatchEndpoint GetOrCreate(IMessagingConfigurationContext context, Uri address)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(address);

        var changed = false;
        DispatchEndpoint? resolved = null;

        lock (_lock)
        {
            if (TryGetEndpoint(address, out resolved))
            {
                return resolved;
            }

            foreach (var transport in context.Transports)
            {
                if (transport.TryGetDispatchEndpoint(address, out var endpoint))
                {
                    changed = Upsert(endpoint, address);
                    resolved = endpoint;
                    break;
                }
            }

            if (resolved is null)
            {
                foreach (var transport in context.Transports)
                {
                    var configuration = transport.CreateEndpointConfiguration(context, address);
                    if (configuration is not null)
                    {
                        resolved = transport.AddEndpoint(context, configuration);
                        if (!resolved.IsCompleted)
                        {
                            resolved.DiscoverTopology(context);
                            resolved.Complete(context);
                        }

                        changed = Upsert(resolved, address);
                        break;
                    }
                }
            }

            if (resolved is null)
            {
                throw ThrowHelper.NoTransportForAddress(address.ToString());
            }
        }

        if (changed)
        {
            _changeTokens.Rotate();
        }

        return resolved;
    }

    /// <inheritdoc />
    public void AddOrUpdate(DispatchEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        bool changed;
        lock (_lock)
        {
            changed = Upsert(endpoint, null);
        }

        if (changed)
        {
            _changeTokens.Rotate();
        }
    }

    /// <inheritdoc />
    public void AddAddress(DispatchEndpoint endpoint, Uri address)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(address);

        var changed = false;

        lock (_lock)
        {
            if (!_endpoints.TryGetValue(endpoint, out var addresses))
            {
                throw ThrowHelper.EndpointMustBeRegistered();
            }

            if (addresses.Contains(address))
            {
                return; // Already has this address
            }

            _endpoints[endpoint] = addresses.Add(address);
            AddToIndex(_byAddress, address, endpoint);
            changed = true;
        }

        if (changed)
        {
            _changeTokens.Rotate();
        }
    }

    /// <inheritdoc />
    public void Remove(DispatchEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var changed = false;

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
            changed = true;
        }

        if (changed)
        {
            _changeTokens.Rotate();
        }
    }

    private bool TryGetEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        if (_byAddress.TryGetValue(address, out var endpoints))
        {
            endpoint = endpoints.FirstOrDefault();
            return endpoint is not null;
        }

        endpoint = null;
        return false;
    }

    private bool Upsert(DispatchEndpoint endpoint, Uri? resolvedAddress)
    {
        if (!endpoint.IsCompleted)
        {
            throw ThrowHelper.EndpointMustBeCompleted();
        }

        var endpointAddress = endpoint.Address;
        var resourceAddress = endpoint.Destination?.Address;

        if (_endpoints.TryGetValue(endpoint, out var oldAddresses))
        {
            // Build new set of addresses
            var newAddresses = ImmutableHashSet<Uri>.Empty;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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

            var addressesChanged = !oldAddresses.SetEquals(newAddresses);

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
            return addressesChanged;
        }

        // New endpoint
        var addresses = ImmutableHashSet<Uri>.Empty;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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
        return true;
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
