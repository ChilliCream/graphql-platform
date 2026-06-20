using System.Diagnostics.CodeAnalysis;

namespace Mocha;

/// <summary>
/// Provides extension methods for <see cref="Uri"/> objects.
/// </summary>
internal static class UriExtensions
{
    /// <summary>
    /// Attempts to read a transport-local queue name from a URI such as <c>queue:orders</c>.
    /// </summary>
    /// <param name="address">The URI to inspect.</param>
    /// <param name="name">When successful, contains the queue name; otherwise null.</param>
    /// <returns>True when the URI represents a local queue name.</returns>
    public static bool TryGetLocalQueueName(this Uri address, [NotNullWhen(true)] out string? name)
    {
        if (address.Scheme is not "queue")
        {
            name = null;
            return false;
        }

        if (!string.IsNullOrEmpty(address.Host))
        {
            name = null;
            return false;
        }

        var path = address.AbsolutePath;
        if (path.Length == 0)
        {
            name = null;
            return false;
        }

        if (path[0] == '/')
        {
            if (path.Length > 1 && path.IndexOf('/', 1) == -1)
            {
                name = path[1..];
                return true;
            }

            name = null;
            return false;
        }

        if (!path.Contains('/'))
        {
            name = path;
            return true;
        }

        name = null;
        return false;
    }

    /// <summary>
    /// Attempts to extract the resource name from a URI.
    /// </summary>
    /// <param name="address">The URI to extract the resource name from.</param>
    /// <param name="scheme">The expected URI scheme to validate against.</param>
    /// <param name="name">When successful, contains the resource name; otherwise null.</param>
    /// <returns>True if a resource name was successfully extracted; otherwise false.</returns>
    public static bool TryGetResourceName(this Uri address, string scheme, out string? name)
    {
        if (address.Scheme != scheme)
        {
            name = null;
            return false;
        }

        if (!string.IsNullOrEmpty(address.Host))
        {
            name = address.Host;
            return true;
        }

        name = address.AbsolutePath;
        return name.Length > 0 && !name.Contains('/');
    }
}
