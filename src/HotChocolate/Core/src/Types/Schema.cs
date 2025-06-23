using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Relay;

#nullable enable

namespace HotChocolate;

/// <summary>
/// A GraphQL Schema defines the capabilities of a GraphQL server. It
/// exposes all available types and directives on the server, as well as
/// the entry points for query, mutation, and subscription operations.
/// </summary>
public partial class Schema
    : TypeSystemObject<SchemaTypeConfiguration>
    , ISchemaDefinition
    , INodeIdRuntimeTypeLookup
{
    /// <summary>
    /// Gets the GraphQL object type that represents the query root.
    /// </summary>
    public ObjectType QueryType { get; private set; } = null!;

    IObjectTypeDefinition ISchemaDefinition.QueryType => QueryType;

    /// <summary>
    /// Gets the GraphQL object type that represents the mutation root.
    /// </summary>
    public ObjectType? MutationType { get; private set; }

    IObjectTypeDefinition? ISchemaDefinition.MutationType => MutationType;

    /// <summary>
    /// Gets the GraphQL object type that represents the subscription root.
    /// </summary>
    public ObjectType? SubscriptionType { get; private set; }

    IObjectTypeDefinition? ISchemaDefinition.SubscriptionType => SubscriptionType;

    /// <summary>
    /// Gets all the schema types.
    /// </summary>
    public TypeDefinitionCollection Types { get; private set; } = null!;

    IReadOnlyTypeDefinitionCollection ISchemaDefinition.Types => Types;

    /// <summary>
    /// Gets all the directives that are supported by this schema.
    /// </summary>
    public DirectiveTypeCollection DirectiveTypes { get; private set; } = null!;

    IReadOnlyDirectiveDefinitionCollection ISchemaDefinition.DirectiveDefinitions
        => DirectiveTypes.AsReadOnlyDirectiveCollection();

    /// <summary>
    /// Gets the schema directives.
    /// </summary>
    /// <value></value>
    public DirectiveCollection Directives { get; private set; } = null!;

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => Directives.AsReadOnlyDirectiveCollection();

    /// <summary>
    /// Gets the global schema services.
    /// </summary>
    public IServiceProvider Services { get; internal set; } = null!;

    /// <summary>
    /// Specifies the time the schema was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns the GraphQL object type for the given <paramref name="operation"/>.
    /// </summary>
    /// <param name="operation">
    /// The operation type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL object type for the given <paramref name="operation"/>
    /// </returns>
    public ObjectType GetOperationType(OperationType operation)
    {
        var type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new ArgumentException(nameof(operation))
        };

        if (type is null)
        {
            throw new InvalidOperationException(
                $"The specified operation type `{operation}` is not supported.");
        }

        return type;
    }

    IObjectTypeDefinition ISchemaDefinition.GetOperationType(OperationType operation)
        => GetOperationType(operation);

    /// <summary>
    /// Tries to get the GraphQL object type for the given <paramref name="operation"/>.
    /// </summary>
    /// <param name="operation">
    /// The operation type.
    /// </param>
    /// <param name="type">
    /// The GraphQL object type for the given <paramref name="operation"/>.
    /// </param>
    /// <returns>
    /// Returns <c>true</c>, if the GraphQL object type for the given
    /// <paramref name="operation"/> was found, <c>false</c> otherwise.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// The specified operation type is not supported.
    /// </exception>
    public bool TryGetOperationType(
        OperationType operation,
        [NotNullWhen(true)] out ObjectType? type)
    {
        type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new NotSupportedException()
        };
        return type is not null;
    }

    bool ISchemaDefinition.TryGetOperationType(
        OperationType operation,
        [NotNullWhen(true)] out IObjectTypeDefinition? type)
    {
        type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new NotSupportedException()
        };
        return type is not null;
    }

    /// <summary>
    /// Gets the possible object types to
    /// an abstract type (union type or interface type).
    /// </summary>
    /// <param name="abstractType">The abstract type.</param>
    /// <returns>
    /// Returns a collection with all possible object types
    /// for the given abstract type.
    /// </returns>
    public IReadOnlyList<ObjectType> GetPossibleTypes(ITypeDefinition abstractType)
    {
        ArgumentNullException.ThrowIfNull(abstractType);
        return _possibleTypes.TryGetValue(abstractType.Name, out var types) ? types : [];
    }

    IEnumerable<IObjectTypeDefinition> ISchemaDefinition.GetPossibleTypes(ITypeDefinition abstractType)
        => GetPossibleTypes(abstractType);

    /// <inheritdoc />
    public IEnumerable<INameProvider> GetAllDefinitions()
    {
        foreach (var type in Types)
        {
            yield return type;
        }

        foreach (var directive in DirectiveTypes)
        {
            yield return directive;
        }
    }

    /// <summary>
    /// Tries to get the .net type representation of a schema.
    /// </summary>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="runtimeType">The resolved .net type.</param>
    /// <returns>
    /// <c>true</c>, if a .net type was found that was bound
    /// the specified schema type, <c>false</c> otherwise.
    /// </returns>
    public bool TryGetRuntimeType(string typeName, [NotNullWhen(true)] out Type? runtimeType)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        if (Types.TryGetType(typeName, out var type)
            && type is IHasRuntimeType ct
            && ct.RuntimeType != typeof(object))
        {
            runtimeType = ct.RuntimeType;
            return true;
        }

        runtimeType = null;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the .NET type of the id field from an object type that implements the Node interface.
    /// </summary>
    public Type? GetNodeIdRuntimeType(string typeName)
    {
        if (Types.TryGetType<ObjectType>(typeName, out var type)
            && type.IsImplementing("Node")
            && type.Fields.TryGetField("id", out var field))
        {
            return field.RuntimeType;
        }

        return null;
    }

    /// <summary>
    /// Creates a schema document from the current schema.
    /// </summary>
    public DocumentNode ToSyntaxNode(bool includeSpecScalars = false)
    {
        _formatter ??= new AggregateSchemaDocumentFormatter(
            Services.GetService<IEnumerable<ISchemaDocumentFormatter>>());
        var document = SchemaPrinter.PrintSchema(this, includeSpecScalars);
        return _formatter.Format(document);
    }

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => ToSyntaxNode();

    /// <summary>
    /// Returns the schema SDL representation.
    /// </summary>
    public override string ToString() => ToSyntaxNode().Print();
}
