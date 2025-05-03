namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL union type definition.
/// </summary>
public interface IUnionTypeDefinition : ITypeDefinition
{
    /// <summary>
    /// Gets the <see cref="IObjectTypeDefinition" /> set of this union type.
    /// </summary>
    IReadOnlyObjectTypeDefinitionCollection Types { get; }
}
