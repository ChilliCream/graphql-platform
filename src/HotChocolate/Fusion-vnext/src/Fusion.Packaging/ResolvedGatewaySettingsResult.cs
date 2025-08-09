using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Represents the result of attempting to resolve gateway settings from a Fusion Archive,
/// including the actual version used and the settings document.
/// </summary>
public readonly struct ResolvedGatewaySettingsResult
{
    /// <summary>
    /// Gets the actual gateway format version that was resolved.
    /// This may be lower than the requested maximum version if a higher version is not available.
    /// Null if no compatible version was found.
    /// </summary>
    public required Version? ActualVersion { get; init; }

    /// <summary>
    /// Gets a value indicating whether gateway settings were successfully resolved.
    /// When true, both ActualVersion and Settings are guaranteed to be non-null.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Settings), nameof(ActualVersion))]
    public required bool IsResolved { get; init; }

    /// <summary>
    /// Gets the resolved gateway settings as a JSON document.
    /// Contains the configuration for transport profiles, source schema endpoints,
    /// and other gateway runtime settings. Null if resolution failed.
    /// </summary>
    public required JsonDocument? Settings { get; init; }

    /// <summary>
    /// Implicitly converts the result to the actual version that was resolved.
    /// Returns null if no version was resolved.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>The actual version or null.</returns>
    public static implicit operator Version?(ResolvedGatewaySettingsResult result)
        => result.ActualVersion;

    /// <summary>
    /// Implicitly converts the result to a boolean indicating resolution success.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>True if settings were resolved, false otherwise.</returns>
    public static implicit operator bool(ResolvedGatewaySettingsResult result)
        => result.IsResolved;

    /// <summary>
    /// Implicitly converts the result to the resolved settings JSON document.
    /// Returns null if resolution failed.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>The settings document or null.</returns>
    public static implicit operator JsonDocument?(ResolvedGatewaySettingsResult result)
        => result.Settings;
}
