using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Relay;

#nullable enable

namespace HotChocolate;

/// <summary>
/// A GraphQL Schema defines the capabilities of a GraphQL server. It
/// exposes all available types and directives on the server, as well as
/// the entry points for query, mutation, and subscription operations.
/// </summary>
public partial class Schema
    : TypeSystemObjectBase<SchemaTypeDefinition>
    , ISchema
{
    private readonly DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private SchemaTypes _types = default!;
    private FrozenDictionary<string, DirectiveType> _directiveTypes = default!;
    private AggregateSchemaDocumentFormatter? _formatter;

    /// <summary>
    /// Gets the schema directives.
    /// </summary>
    /// <value></value>
    public IDirectiveCollection Directives { get; private set; } = default!;

    /// <summary>
    /// Gets the global schema services.
    /// </summary>
    public IServiceProvider Services { get; private set; } = default!;

    /// <summary>
    /// The type that query operations will be rooted at.
    /// </summary>
    public ObjectType QueryType => _types.QueryType;

    /// <summary>
    /// If this server supports mutation, the type that
    /// mutation operations will be rooted at.
    /// </summary>
    public ObjectType? MutationType => _types.MutationType;

    /// <summary>
    /// If this server support subscription, the type that
    /// subscription operations will be rooted at.
    /// </summary>
    public ObjectType? SubscriptionType => _types.SubscriptionType;

    /// <summary>
    /// Gets all the schema types.
    /// </summary>
    public IReadOnlyCollection<INamedType> Types => _types.GetTypes();

    /// <summary>
    /// Gets all the directives that are supported by this schema.
    /// </summary>
    public IReadOnlyCollection<DirectiveType> DirectiveTypes { get; private set; } = default!;

    /// <summary>
    /// Gets the schema features.
    /// </summary>
    public IFeatureCollection Features { get; private set; } = default!;

    /// <summary>
    /// Specifies the time the schema was created.
    /// </summary>
    public DateTimeOffset CreatedAt => _createdAt;

    /// <summary>
    /// Gets the default schema name.
    /// </summary>
    public static string DefaultName => "_Default";

    /// <summary>
    /// Gets a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="typeName">The name of the type.</param>
    /// <returns>The type.</returns>
    /// <exception cref="ArgumentException">
    /// The specified type does not exist or
    /// is not of the specified type kind.
    /// </exception>
    [return: NotNull]
    public T GetType<T>(string typeName)
        where T : INamedType
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        return _types.GetType<T>(typeName);
    }

    /// <summary>
    /// Tries to get a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="type">The resolved type.</param>
    /// <returns>
    /// <c>true</c>, if a type with the name exists and is of the specified
    /// kind, <c>false</c> otherwise.
    /// </returns>
    public bool TryGetType<T>(string typeName, [MaybeNullWhen(false)] out T type)
        where T : INamedType
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        return _types.TryGetType(typeName, out type);
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
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        return _types.TryGetClrType(typeName, out runtimeType);
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
    public IReadOnlyList<ObjectType> GetPossibleTypes(INamedType abstractType)
    {
        if (abstractType is null)
        {
            throw new ArgumentNullException(nameof(abstractType));
        }

        if (_types.TryGetPossibleTypes(abstractType.Name, out var types))
        {
            return types;
        }

        return Array.Empty<ObjectType>();
    }

    /// <summary>
    /// Gets a directive type by its name.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns directive type that was resolved by the given name
    /// or <c>null</c> if there is no directive with the specified name.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified directive type does not exist.
    /// </exception>
    public DirectiveType GetDirectiveType(string directiveName)
    {
        if (string.IsNullOrEmpty(directiveName))
        {
            throw new ArgumentNullException(nameof(directiveName));
        }

        if (_directiveTypes.TryGetValue(directiveName, out var type))
        {
            return type;
        }

        throw new ArgumentException(
            string.Format(TypeResources.Schema_GetDirectiveType_DoesNotExist, directiveName),
            nameof(directiveName));
    }

    /// <summary>
    /// Tries to get a directive type by its name.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <param name="directiveType">
    /// The directive type that was resolved by the given name
    /// or <c>null</c> if there is no directive with the specified name.
    /// </param>
    /// <returns>
    /// <c>true</c>, if a directive type with the specified
    /// name exists; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetDirectiveType(
        string directiveName,
        [NotNullWhen(true)] out DirectiveType? directiveType)
    {
        if (string.IsNullOrEmpty(directiveName))
        {
            throw new ArgumentNullException(nameof(directiveName));
        }

        return _directiveTypes.TryGetValue(directiveName, out directiveType);
    }

    Type? INodeIdRuntimeTypeLookup.GetNodeIdRuntimeType(string typeName)
    {
        if (TryGetType<ObjectType>(typeName, out var type)
            && type.IsImplementing("Node")
            && type.Fields.TryGetField("id", out var field))
        {
            return field.RuntimeType;
        }

        return null;
    }

    /// <summary>
    /// Generates a schema document.
    /// </summary>
    public DocumentNode ToDocument(bool includeSpecScalars = false)
    {
        _formatter ??= new AggregateSchemaDocumentFormatter(
            Services.GetService<IEnumerable<ISchemaDocumentFormatter>>());
        var document = SchemaPrinter.PrintSchema(this, includeSpecScalars);
        return _formatter.Format(document);
    }

    /// <summary>
    /// Returns the schema SDL representation.
    /// </summary>
    public string Print() => ToDocument().Print();

    /// <summary>
    /// Returns the schema SDL representation.
    /// </summary>
    public override string ToString() => Print();
}
