namespace HotChocolate.Language;

/// <summary>
/// Represents type definition syntax.
/// </summary>
public interface ITypeDefinitionNode
    : ITypeSystemDefinitionNode
    , INamedSyntaxNode
{
    /// <summary>
    /// Gets the description of the type definition.
    /// </summary>
    StringValueNode? Description { get; }
}
