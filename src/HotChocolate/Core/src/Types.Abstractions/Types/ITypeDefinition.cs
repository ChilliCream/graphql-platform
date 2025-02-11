namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL type definition.
/// </summary>
public interface ITypeDefinition
    : IType
    , INameProvider
    , IDescriptionProvider
    , IDirectivesProvider
    , ISyntaxNodeProvider
{
    /// <summary>
    /// Determines whether an instance of a specified type <paramref name="type" />
    /// can be assigned to a variable of the current type.
    /// </summary>
    bool IsAssignableFrom(ITypeDefinition type);
}
