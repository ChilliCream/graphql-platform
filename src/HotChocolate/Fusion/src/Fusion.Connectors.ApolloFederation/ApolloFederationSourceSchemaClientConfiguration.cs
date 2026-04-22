namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Configuration for an Apollo Federation subgraph source schema client
/// that sends queries over HTTP using the <c>_entities</c> protocol.
/// </summary>
public sealed class ApolloFederationSourceSchemaClientConfiguration : ISourceSchemaClientConfiguration
{
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
    /// <param name="lookups">
    /// The lookup field metadata used to rewrite Fusion planner queries into
    /// Apollo Federation <c>_entities</c> queries.
    /// </param>
    /// <param name="entityRequires">
    /// Per-entity-type metadata describing the <c>@require</c> arguments on
    /// each entity field. Enables the rewriter to strip synthetic require
    /// arguments from outgoing queries and to inject the bound variable
    /// values into the <c>_entities</c> representation body.
    /// </param>
    /// <param name="supportedOperations">The supported operation types.</param>
    internal ApolloFederationSourceSchemaClientConfiguration(
        string name,
        string httpClientName,
        Uri baseAddress,
        IReadOnlyDictionary<string, LookupFieldInfo> lookups,
        IReadOnlyDictionary<string, EntityRequiresInfo>? entityRequires = null,
        SupportedOperationType supportedOperations = SupportedOperationType.Query | SupportedOperationType.Mutation)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(httpClientName);
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentNullException.ThrowIfNull(lookups);

        Name = name;
        HttpClientName = httpClientName;
        BaseAddress = baseAddress;
        Lookups = lookups;
        EntityRequires = entityRequires
            ?? new Dictionary<string, EntityRequiresInfo>(StringComparer.Ordinal);
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
    /// into Apollo Federation <c>_entities</c> queries.
    /// </summary>
    internal IReadOnlyDictionary<string, LookupFieldInfo> Lookups { get; }

    /// <summary>
    /// Gets the per-entity-type <c>@require</c> argument metadata keyed by
    /// entity type name (e.g. <c>"Product"</c>).
    /// </summary>
    internal IReadOnlyDictionary<string, EntityRequiresInfo> EntityRequires { get; }

    /// <inheritdoc />
    public SupportedOperationType SupportedOperations { get; }
}
