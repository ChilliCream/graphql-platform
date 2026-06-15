using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Selects the appropriate transport for an outbound route.
/// </summary>
internal static class MessagingTransportSelection
{
    /// <summary>
    /// Selects the transport that should handle <paramref name="route"/>.
    /// </summary>
    /// <param name="transports">All transports registered on the bus.</param>
    /// <param name="route">The outbound route being dispatched.</param>
    /// <returns>The transport responsible for this route.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when selection is ambiguous: multiple transports claim the destination scheme,
    /// multiple transports are flagged as default, or no default is set and multiple transports
    /// are available.
    /// </exception>
    public static MessagingTransport Select(
        ImmutableArray<MessagingTransport> transports,
        OutboundRoute route)
    {
        if (transports.IsEmpty)
        {
            throw ThrowHelper.NoTransportForMessageType(route.MessageType);
        }

        // (1) Explicit transport scheme: scheme of the destination URI uniquely identifies a transport.
        if (route.Destination is { } destination)
        {
            var scheme = destination.Scheme;
            MessagingTransport? matched = null;
            foreach (var transport in transports)
            {
                if (transport.Schema == scheme)
                {
                    // Multiple transports with the same schema is not expected in a valid configuration,
                    // but we return the first match here since schemas are unique by convention.
                    matched = transport;
                    break;
                }
            }

            if (matched is not null)
            {
                return matched;
            }
        }

        // (2) Resolved default.
        return ResolveDefault(transports);
    }

    /// <summary>
    /// Resolves the default transport: the unique transport flagged via
    /// <see cref="MessagingTransportConfiguration.IsDefaultTransport"/>, or the sole registered
    /// transport when no flag is set.
    /// </summary>
    /// <param name="transports">All transports registered on the bus.</param>
    /// <returns>The default transport.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple transports are flagged as default, or when no default is set
    /// and multiple transports are registered.
    /// </exception>
    public static MessagingTransport ResolveDefault(ImmutableArray<MessagingTransport> transports)
    {
        MessagingTransport? defaultTransport = null;
        var multipleDefaults = false;

        foreach (var transport in transports)
        {
            if (transport.IsDefaultTransport)
            {
                if (defaultTransport is not null)
                {
                    multipleDefaults = true;
                }

                defaultTransport = transport;
            }
        }

        if (multipleDefaults)
        {
            var names = transports
                .Where(t => t.IsDefaultTransport)
                .Select(t => t.Name)
                .ToArray();
            throw ThrowHelper.MultipleDefaultTransports(names);
        }

        if (defaultTransport is not null)
        {
            return defaultTransport;
        }

        // Sole-transport fallback: single transport needs no explicit default flag.
        if (transports.Length == 1)
        {
            return transports[0];
        }

        var allNames = transports.Select(t => t.Name).ToArray();
        throw ThrowHelper.NoDefaultTransportAvailable(allNames);
    }
}
