using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate;

public interface ISchemaDefinition : INameProvider, IDescriptionProvider, ISyntaxNodeProvider
{

    /// <summary>
    /// The type that query operations will be rooted at.
    /// </summary>
    IObjectTypeDefinition QueryType { get; }

    /// <summary>
    /// If this server supports mutation, the type that
    /// mutation operations will be rooted at.
    /// </summary>
    IObjectTypeDefinition? MutationType { get; }

    /// <summary>
    /// If this server support subscription, the type that
    /// subscription operations will be rooted at.
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
}
