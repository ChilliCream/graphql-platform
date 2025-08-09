using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Represents the result of attempting to resolve a gateway schema version from a Fusion Archive.
/// </summary>
public readonly struct ResolvedGatewaySchemaResult
{
    /// <summary>
    /// Gets the actual gateway format version that was resolved.
    /// This may be lower than the requested maximum version if a higher version is not available.
    /// Null if no compatible version was found.
    /// </summary>
    public required Version? ActualVersion { get; init; }

    /// <summary>
    /// Gets a value indicating whether a gateway schema version was successfully resolved.
    /// When true, ActualVersion is guaranteed to be non-null.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ActualVersion))]
    public required bool IsResolved { get; init; }

    /// <summary>
    /// Implicitly converts the result to the actual version that was resolved.
    /// Returns null if no version was resolved.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>The actual version or null.</returns>
    public static implicit operator Version?(ResolvedGatewaySchemaResult result)
        => result.ActualVersion;

    /// <summary>
    /// Implicitly converts the result to a boolean indicating resolution success.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>True if a schema version was resolved, false otherwise.</returns>
    public static implicit operator bool(ResolvedGatewaySchemaResult result)
        => result.IsResolved;
}
