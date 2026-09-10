namespace Mocha;

/// <summary>
/// Provides extension methods for <see cref="Uri"/> objects.
/// </summary>
internal static class UriExtensions
{
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
