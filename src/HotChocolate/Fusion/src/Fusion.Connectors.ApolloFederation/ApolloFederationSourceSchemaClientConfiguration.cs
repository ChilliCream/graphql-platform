using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Configuration for an Apollo Federation subgraph source schema client
/// that sends queries over HTTP using the <c>_entities</c> protocol.
/// </summary>
public sealed class ApolloFederationSourceSchemaClientConfiguration
    : ISourceSchemaClientConfiguration
    , INeedsCompletion
{
    private Dictionary<string, LookupFieldInfo> _lookups = new(StringComparer.Ordinal);
    private Dictionary<string, EntityRequiresInfo> _entityRequires = new(StringComparer.Ordinal);
    private FederationQueryRewriter? _queryRewriter;

    /// <summary>
    /// Initializes a new instance of <see cref="ApolloFederationSourceSchemaClientConfiguration"/>.
    /// </summary>
    /// <param name="name">The name of the source schema.</param>
    /// <param name="httpClientName">
    /// The name of the <see cref="HttpClient"/> to resolve from
    /// <see cref="IHttpClientFactory"/>.
    /// </param>
    /// <param name="baseAddress">
    /// The base address of the Apollo Federation subgraph endpoint.
    /// </param>
    /// <param name="supportedOperations">The supported operation types.</param>
    internal ApolloFederationSourceSchemaClientConfiguration(
        string name,
        string httpClientName,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.Query | SupportedOperationType.Mutation)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(httpClientName);
        ArgumentNullException.ThrowIfNull(baseAddress);

        Name = name;
        HttpClientName = httpClientName;
        BaseAddress = baseAddress;
        SupportedOperations = supportedOperations;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets the name of the underlying HTTP client.
    /// </summary>
    public string HttpClientName { get; }

    /// <summary>
    /// Gets the base address of the Apollo Federation subgraph endpoint.
    /// </summary>
    public Uri BaseAddress { get; }

    /// <summary>
    /// Gets the lookup field metadata used to rewrite Fusion planner queries
    /// into Apollo Federation <c>_entities</c> queries. Populated during schema
    /// completion from the runtime <see cref="Lookup"/> metadata on
    /// <see cref="FusionSchemaDefinition"/>.
    /// </summary>
    internal IReadOnlyDictionary<string, LookupFieldInfo> Lookups => _lookups;

    /// <summary>
    /// Gets the per-entity-type <c>@require</c> argument metadata keyed by
    /// entity type name (e.g. <c>"Product"</c>). Populated during schema
    /// completion from the runtime <see cref="FieldRequirements"/> metadata on
    /// <see cref="FusionSchemaDefinition"/>.
    /// </summary>
    internal IReadOnlyDictionary<string, EntityRequiresInfo> EntityRequires => _entityRequires;

    /// <summary>
    /// Gets the per-source-schema <see cref="FederationQueryRewriter"/> that
    /// rewrites planner-emitted lookup queries into Apollo Federation
    /// <c>_entities</c> queries. Available after schema completion.
    /// </summary>
    internal FederationQueryRewriter QueryRewriter
        => _queryRewriter
            ?? throw new InvalidOperationException(
                $"The query rewriter for source schema '{Name}' is not available before schema completion.");

    /// <inheritdoc />
    public SupportedOperationType SupportedOperations { get; }

    void INeedsCompletion.Complete(FusionSchemaDefinition schema, CompositeSchemaBuilderContext context)
    {
        var lookups = new Dictionary<string, LookupFieldInfo>(StringComparer.Ordinal);
        var entityRequires = new Dictionary<string, EntityRequiresInfo>(StringComparer.Ordinal);

        foreach (var type in schema.Types.AsEnumerable(allowInaccessibleFields: true))
        {
            if (type is not FusionObjectTypeDefinition objectType)
            {
                continue;
            }

            if (objectType.Sources.TryGetMember(Name, out var sourceObjectType))
            {
                ProjectLookups(sourceObjectType, lookups);
            }

            ProjectEntityRequires(objectType, entityRequires);
        }

        _lookups = lookups;
        _entityRequires = entityRequires;
        _queryRewriter = new FederationQueryRewriter(_lookups, _entityRequires);
    }

    private static void ProjectLookups(
        SourceObjectType sourceObjectType,
        Dictionary<string, LookupFieldInfo> lookups)
    {
        foreach (var lookup in sourceObjectType.Lookups)
        {
            var argumentToKeyFieldMap = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var i = 0; i < lookup.Arguments.Length; i++)
            {
                var argument = lookup.Arguments[i];
                var selection = lookup.Fields[i];
                argumentToKeyFieldMap[argument.Name] = LookupArgumentPathMapper.Map(selection);
            }

            lookups[lookup.FieldName] = new LookupFieldInfo
            {
                EntityTypeName = lookup.FieldType.Name,
                ArgumentToKeyFieldMap = argumentToKeyFieldMap
            };
        }
    }

    private void ProjectEntityRequires(
        FusionObjectTypeDefinition objectType,
        Dictionary<string, EntityRequiresInfo> entityRequires)
    {
        Dictionary<string, IReadOnlyDictionary<string, string>>? fieldMap = null;

        foreach (var field in objectType.Fields.AsEnumerable(allowInaccessibleFields: true))
        {
            if (!field.Sources.TryGetMember(Name, out var sourceField)
                || sourceField.Requirements is null)
            {
                continue;
            }

            var requirements = sourceField.Requirements;
            var requires = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var i = 0; i < requirements.Arguments.Length; i++)
            {
                var argument = requirements.Arguments[i];
                var selection = requirements.Fields[i];

                if (selection is null)
                {
                    continue;
                }

                requires[argument.Name] = LookupArgumentPathMapper.Map(selection);
            }

            if (requires.Count > 0)
            {
                fieldMap ??= new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
                fieldMap[field.Name] = requires;
            }
        }

        if (fieldMap is { Count: > 0 })
        {
            entityRequires[objectType.Name] = new EntityRequiresInfo
            {
                Fields = fieldMap
            };
        }
    }
}
