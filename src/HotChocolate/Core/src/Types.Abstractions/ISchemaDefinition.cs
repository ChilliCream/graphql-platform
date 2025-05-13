using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate;

/// <summary>
/// A GraphQL Schema defines the capabilities of a GraphQL server. It
/// exposes all available types and directives on the server, as well as
/// the entry points for query, mutation, and subscription operations.
/// </summary>
public interface ISchemaDefinition
    : INameProvider
    , IDescriptionProvider
    , IDirectivesProvider
    , IFeatureProvider
    , ISyntaxNodeProvider
{
    /// <summary>
    /// Gets the GraphQL object type that represents the query root.
    /// </summary>
    IObjectTypeDefinition QueryType { get; }

    /// <summary>
    /// Gets the GraphQL object type that represents the mutation root.
    /// </summary>
    IObjectTypeDefinition? MutationType { get; }

    /// <summary>
    /// Gets the GraphQL object type that represents the subscription root.
    /// </summary>
    IObjectTypeDefinition? SubscriptionType { get; }

    /// <summary>
    /// Gets all the schema types.
    /// </summary>
    IReadOnlyTypeDefinitionCollection Types { get; }

    /// <summary>
    /// Gets all the directive definitions that are supported by this schema.
    /// </summary>
    IReadOnlyDirectiveDefinitionCollection DirectiveDefinitions { get; }

    /// <summary>
    /// Gets the operation type for the given operation type.
    /// </summary>
    /// <param name="operationType">
    /// The operation type.
    /// </param>
    /// <returns>
    /// Returns the operation type for the given operation type.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The operation type is not supported.
    /// </exception>
    IObjectTypeDefinition GetOperationType(OperationType operationType);

    /// <summary>
    /// Gets the possible object types to
    /// an abstract type (union type or interface type).
    /// </summary>
    /// <param name="abstractType">The abstract type.</param>
    /// <returns>
    /// Returns a collection with all possible object types
    /// for the given abstract type.
    /// </returns>
    IEnumerable<IObjectTypeDefinition> GetPossibleTypes(ITypeDefinition abstractType);

    /// <summary>
    /// Gets all the definitions that are part of this schema (type definitions and directive definitions).
    /// </summary>
    IEnumerable<INameProvider> GetAllDefinitions();

    /// <summary>
    /// Returns a string that represents the current schema.
    /// </summary>
    /// <returns>
    /// A string that represents the current schema.
    /// </returns>
    string ToString();

    /// <summary>
    /// The default name of a schema.
    /// </summary>
    public static string DefaultName => "default";
}
