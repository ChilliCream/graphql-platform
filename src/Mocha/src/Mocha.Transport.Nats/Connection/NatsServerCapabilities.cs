using System.Globalization;
using System.Text.RegularExpressions;

namespace Mocha.Transport.Nats;

/// <summary>
/// Describes which version-gated JetStream features the connected server supports.
/// </summary>
public sealed partial class NatsServerCapabilities
{
    private static readonly Version s_messageTtlVersion = new(2, 11);
    private static readonly Version s_messageScheduleVersion = new(2, 12);

    private NatsServerCapabilities(Version? version)
    {
        Version = version;
    }

    /// <summary>
    /// Gets the server version, or <see langword="null"/> when it could not be determined.
    /// </summary>
    public Version? Version { get; }

    /// <summary>
    /// Gets a value indicating whether per-message TTL headers are supported, requiring server 2.11.
    /// </summary>
    public bool SupportsMessageTtl => Version is null || Version >= s_messageTtlVersion;

    /// <summary>
    /// Gets a value indicating whether message schedules are supported, requiring server 2.12.
    /// </summary>
    public bool SupportsMessageSchedules => Version is null || Version >= s_messageScheduleVersion;

    /// <summary>
    /// Reads the capabilities from a reported server version string.
    /// </summary>
    /// <param name="serverVersion">The version reported by the server, for example <c>2.12.1</c>.</param>
    /// <returns>
    /// The capabilities. An unparsable or absent version reports every feature as supported.
    /// </returns>
    // Assuming capable means a server reporting an unfamiliar version string fails on the real
    // JetStream call with the server's own error, rather than being pre-emptively rejected here.
    public static NatsServerCapabilities FromServerVersion(string? serverVersion)
    {
        if (string.IsNullOrWhiteSpace(serverVersion))
        {
            return new NatsServerCapabilities(null);
        }

        var match = VersionPrefixRegex().Match(serverVersion);

        if (!match.Success)
        {
            return new NatsServerCapabilities(null);
        }

        var major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
        var minor = int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture);

        return new NatsServerCapabilities(new Version(major, minor));
    }

    [GeneratedRegex(@"^v?(?<major>\d+)\.(?<minor>\d+)")]
    private static partial Regex VersionPrefixRegex();
}
