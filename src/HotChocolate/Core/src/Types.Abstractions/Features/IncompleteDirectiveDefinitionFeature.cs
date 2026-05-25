namespace HotChocolate.Features;

/// <summary>
/// Represents a feature that indicates whether a directive definition is incomplete.
/// </summary>
public sealed class IncompleteDirectiveDefinitionFeature
{
    /// <summary>
    /// Gets a value indicating whether the directive definition is incomplete.
    /// </summary>
    public bool IsIncomplete { get; init; }
}
