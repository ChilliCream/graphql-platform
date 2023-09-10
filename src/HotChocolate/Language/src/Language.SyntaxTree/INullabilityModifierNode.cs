namespace HotChocolate.Language;

/// <summary>
/// Represents the nullability modifier which is expressed by ! or ?
/// </summary>
public interface INullabilityModifierNode : INullabilityNode
{
    /// <summary>
    /// Gets the inner nullability status.
    /// </summary>
    public new ListNullabilityNode? Element { get; }
}
