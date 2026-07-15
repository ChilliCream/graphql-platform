namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents one policy application on a Fusion object or field coordinate.
/// </summary>
public sealed record PolicyApplication
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the behavior used when this policy denies an entity.
    /// </summary>
    public required PolicyDenialBehavior OnDenied { get; init; }
}
