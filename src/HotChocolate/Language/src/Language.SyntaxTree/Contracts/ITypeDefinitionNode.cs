namespace HotChocolate.Language;

/// <summary>
/// Represents type definition syntax.
/// </summary>
public interface ITypeDefinitionNode
    : ITypeSystemDefinitionNode
    , INamedSyntaxNode
{
    StringValueNode? Description { get; }
}
