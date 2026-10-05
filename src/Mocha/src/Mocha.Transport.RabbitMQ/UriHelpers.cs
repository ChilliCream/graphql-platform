using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.WebUtilities;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Helper methods for extracting RabbitMQ-specific parameters from URIs.
/// </summary>
internal static class UriHelpers
{
    /// <summary>
    /// Attempts to extract a routing key from the URI query string parameter named "routingKey".
    /// </summary>
    /// <param name="uri">The URI to parse.</param>
    /// <param name="routingKey">When this method returns <c>true</c>, contains the decoded routing key value.</param>
    /// <returns><c>true</c> if a routing key was found in the query string; otherwise, <c>false</c>.</returns>
    public static bool TryGetRoutingKey(this Uri uri, [NotNullWhen(true)] out string? routingKey)
    {
        if (uri.Query is not "" and not null)
        {
            var enumerable = new QueryStringEnumerable(uri.Query);
            foreach (var value in enumerable)
            {
                if (value.EncodedName.Span is "routingKey")
                {
                    routingKey = new string(value.DecodeValue().Span);
                    return true;
                }
            }
        }
        routingKey = null;
        return false;
    }

    /// <summary>
    /// Attempts to parse an address under the given topology address into its resource kind and name.
    /// Everything after the kind segment is the name, because a queue or exchange name may contain a slash.
    /// </summary>
    /// <param name="address">The address to parse.</param>
    /// <param name="topologyAddress">The transport topology address the resource must belong to.</param>
    /// <param name="kind">When this method returns <c>true</c>, contains the single-character resource kind, such as <c>q</c> or <c>e</c>.</param>
    /// <param name="name">When this method returns <c>true</c>, contains the unescaped resource name.</param>
    /// <returns><c>true</c> if the address names a resource under the topology address; otherwise, <c>false</c>.</returns>
    public static bool TryParseTopologyAddress(
        this Uri address,
        Uri topologyAddress,
        out char kind,
        out string name)
    {
        kind = default;
        name = string.Empty;

        // A valid resource path is not enough to identify this transport. For example,
        // rabbitmq://other-host:5672/tenant-a/q/orders must not be claimed by this topology.
        if (!address.Scheme.EqualsOrdinalIgnoreCase(topologyAddress.Scheme)
            || !address.Host.EqualsOrdinalIgnoreCase(topologyAddress.Host)
            || address.Port != topologyAddress.Port)
        {
            return false;
        }

        // Normalize only the topology path: "/" becomes empty and "/tenant-a/" becomes
        // "/tenant-a". Keep the address path unchanged while removing the topology prefix below.
        var basePath = topologyAddress.AbsolutePath.AsSpan().TrimEnd('/');
        var path = address.AbsolutePath.AsSpan();
        ReadOnlySpan<char> relativePath;

        if (basePath.IsEmpty)
        {
            // A root topology consumes exactly one structural slash:
            // "/q/orders" becomes "q/orders". The shorthand "rabbitmq:q/orders" has no
            // leading slash and is handled by the transport-specific branch of the caller.
            if (path.IsEmpty || path[0] is not '/')
            {
                return false;
            }

            relativePath = path[1..];
        }
        else
        {
            // Remove the exact virtual-host path and its separator:
            // "/tenant-a/q/orders" becomes "q/orders". Requiring the separator prevents
            // "/tenant-a" from matching "/tenant-a-other/q/orders".
            if (path.Length <= basePath.Length
                || !path.StartsWith(basePath, StringComparison.Ordinal)
                || path[basePath.Length] is not '/')
            {
                return false;
            }

            relativePath = path[(basePath.Length + 1)..];
        }

        // Require "{kind}/{name}", where kind is one character. Everything after the first
        // slash is the name, so a name containing a slash, such as "q/nested/queue", stays intact.
        if (relativePath.Length < 3 || relativePath[1] is not '/')
        {
            return false;
        }

        kind = relativePath[0];
        // AbsolutePath is escaped, so "q/space%20name" must produce "space name".
        name = Uri.UnescapeDataString(relativePath[2..].ToString());
        return true;
    }
}
