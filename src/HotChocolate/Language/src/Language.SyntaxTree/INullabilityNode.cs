namespace HotChocolate.Language;

/// <summary>
/// Represents the nullability status which is expressed by !, ? or []
/// </summary>
public interface INullabilityNode : ISyntaxNode
{
    /// <summary>
    /// Gets the inner nullability status.
    /// </summary>
    public INullabilityNode? Element { get; }
}
