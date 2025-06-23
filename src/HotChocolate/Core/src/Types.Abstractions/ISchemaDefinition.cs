using System.Diagnostics.CodeAnalysis;
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
    /// Gets the schema services.
    /// </summary>
    IServiceProvider Services { get; }

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
    /// Gets the object type that represents the given <paramref name="operation"/>.
    /// </summary>
    /// <param name="operation">
    /// The operation for which the object type shall be returned.
    /// </param>
    /// <returns>
    /// Returns the object type that represents the given <paramref name="operation"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified operation is not supported.
    /// </exception>
    IObjectTypeDefinition GetOperationType(OperationType operation);

    /// <summary>
    /// Tries to get the object type that represents the given <paramref name="operation"/>.
    /// </summary>
    /// <param name="operation">
    /// The operation for which the object type shall be returned.
    /// </param>
    /// <param name="type">
    /// The object type that represents the given <paramref name="operation"/>.
    /// </param>
    /// <returns>
    /// Returns true if the operation type was found; otherwise, false.
    /// </returns>
    bool TryGetOperationType(
        OperationType operation,
        [NotNullWhen(true)] out IObjectTypeDefinition? type);

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
    /// Returns the schema SDL representation of the current schema definition.
    /// </summary>
    /// <returns>
    /// Returns the schema SDL representation of the current schema definition.
    /// </returns>
    string ToString();

    /// <summary>
    /// The default name of a schema.
    /// </summary>
    public static string DefaultName => "_Default";
}
