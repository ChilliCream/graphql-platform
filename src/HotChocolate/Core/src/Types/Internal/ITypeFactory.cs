using HotChocolate.Types;

namespace HotChocolate.Internal;

/// <summary>
/// Represents a GraphQL type factory.
/// </summary>
public interface ITypeFactory
{
    /// <summary>
    /// Creates a type structure with the <paramref name="typeDefinition"/>.
    /// </summary>
    /// <param name="typeDefinition">
    /// The type definition.
    /// </param>
    /// <returns>
    /// Returns a GraphQL type structure.
    /// </returns>
    IType CreateType(ITypeDefinition typeDefinition);
}
