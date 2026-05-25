using HotChocolate.Features;

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL type definition.
/// </summary>
public interface ITypeDefinition
    : IType
    , INameProvider
    , IDescriptionProvider
    , IDirectivesProvider
    , IFeatureProvider
    , ISyntaxNodeProvider
    , ISchemaCoordinateProvider
    , IRuntimeTypeProvider
{
    /// <summary>
    /// Specifies if this type is an introspection type.
    /// </summary>
    bool IsIntrospectionType => Name.StartsWith("__");

    /// <summary>
    /// Determines whether an instance of a specified type <paramref name="type" />
    /// can be assigned to a variable of the current type.
    /// </summary>
    bool IsAssignableFrom(ITypeDefinition type);
}

/// <summary>
/// Represents a GraphQL output type definition.
/// </summary>
public interface IOutputTypeDefinition : ITypeDefinition, IOutputType;

/// <summary>
/// Represents a GraphQL input type definition.
/// </summary>
public interface IInputTypeDefinition : ITypeDefinition, IInputType;
