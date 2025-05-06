using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate;

/// <summary>
/// A GraphQL Schema defines the capabilities of a GraphQL server. It
/// exposes all available types and directives on the server, as well as
/// the entry points for query, mutation, and subscription operations.
/// </summary>
public interface ISchemaDefinition : INameProvider, IDescriptionProvider, ISyntaxNodeProvider
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
    /// Gets all the directive definitions that are supported by this schema.
    /// </summary>
    IReadOnlyDirectiveCollection Directives { get; }

    /// <summary>
    /// Gets all the schema types.
    /// </summary>
    IReadOnlyTypeDefinitionCollection Types { get; }

    IReadOnlyDirectiveDefinitionCollection DirectiveDefinitions { get; }

    IObjectTypeDefinition? GetOperationType(OperationType operationType);

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
}
