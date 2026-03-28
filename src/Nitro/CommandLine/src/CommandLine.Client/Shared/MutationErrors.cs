namespace ChilliCream.Nitro.Client;

/// <summary>
/// Base type for mutation payload errors.
/// </summary>
public abstract record MutationError(string Message);

public sealed record UnknownMutationError(string Message) : MutationError(Message);

public sealed record OperationsAreNotAllowedError(string Message) : MutationError(Message);

public sealed record ConcurrentOperationError(string Message) : MutationError(Message);

public sealed record UnexpectedProcessingError(string Message) : MutationError(Message);

public sealed record ProcessingTimeoutError(string Message) : MutationError(Message);

public sealed record ReadyTimeoutError(string Message) : MutationError(Message);

public sealed record SchemaVersionSyntaxError(
    string Message,
    int Line,
    int Column,
    int Position)
    : MutationError(Message);

public sealed record ApiNotFoundError(string Message, string ApiId) : MutationError(Message);

public sealed record MockSchemaNonUniqueNameError(string Message, string Name) : MutationError(Message);

public sealed record MockSchemaNotFoundError(string Message) : MutationError(Message);

public sealed record StageNotFoundError(string Message, string Name) : MutationError(Message);

public sealed record SubgraphInvalidError(string Message) : MutationError(Message);

public sealed record InvalidFusionSourceSchemaArchiveError(string Message) : MutationError(Message);

public sealed record InvalidOpenApiCollectionArchiveError(string Message) : MutationError(Message);

public sealed record OpenApiCollectionValidationArchiveError(string Message) : MutationError(Message);

public sealed record InvalidMcpFeatureCollectionArchiveError(string Message) : MutationError(Message);

public sealed record McpFeatureCollectionValidationArchiveError(string Message) : MutationError(Message);

public sealed record SchemaVersionChangeViolationError(IReadOnlyList<SchemaChangeError> Changes)
    : MutationError("Schema version contains change violations.");

public sealed record SchemaChangeViolationError(
    string Message,
    IReadOnlyList<SchemaChangeError> Changes)
    : MutationError(Message);

public enum SchemaChangeSeverityKind
{
    Safe,
    Dangerous,
    Breaking
}

public sealed record SchemaChangeError(
    SchemaChangeSeverityKind Severity,
    string Message,
    IReadOnlyList<SchemaChangeError>? Changes = null);

public sealed record GraphQLSchemaError(string Message, string? Code);

public sealed record InvalidGraphQLSchemaError(
    string Message,
    IReadOnlyList<GraphQLSchemaError> Errors)
    : MutationError(Message);

public sealed record MutationErrorLocation(int Line, int Column);

public sealed record PersistedQueryValidationIssue(
    string Message,
    string? Code,
    string? Path,
    IReadOnlyList<MutationErrorLocation>? Locations);

public sealed record PersistedQueryValidationQuery(
    string Message,
    string Hash,
    IReadOnlyList<string> DeployedTags,
    IReadOnlyList<PersistedQueryValidationIssue> Errors);

public sealed record PersistedQueryValidationClient(string Id, string Name);

public sealed record PersistedQueryValidationError(
    string Message,
    PersistedQueryValidationClient? Client,
    IReadOnlyList<PersistedQueryValidationQuery> Queries)
    : MutationError(Message);

public sealed record StagesHavePublishedDependenciesError(
    string Message,
    IReadOnlyList<StageDependency> Stages)
    : MutationError(Message);

public sealed record StageDependency(
    string Name,
    PublishedSchemaDependency? PublishedSchema,
    IReadOnlyList<PublishedClientDependency> PublishedClients);

public sealed record PublishedSchemaDependency(string Tag);

public sealed record PublishedClientDependency(
    PublishedClientReference Client,
    IReadOnlyList<PublishedClientVersionDependency> PublishedVersions);

public sealed record PublishedClientReference(string Name);

public sealed record PublishedClientVersionDependency(string Tag);

public sealed record OpenApiCollectionReference(string Id, string Name);

public abstract record OpenApiCollectionValidationEntityError(string Message);

public sealed record OpenApiCollectionValidationDocumentError(
    string Message,
    string? Code,
    string? Path,
    IReadOnlyList<MutationErrorLocation>? Locations)
    : OpenApiCollectionValidationEntityError(Message);

public sealed record OpenApiCollectionValidationEntityValidationError(string Message)
    : OpenApiCollectionValidationEntityError(Message);

public abstract record OpenApiCollectionValidationEntity(
    IReadOnlyList<OpenApiCollectionValidationEntityError> Errors);

public sealed record OpenApiCollectionValidationEndpoint(
    string HttpMethod,
    string Route,
    IReadOnlyList<OpenApiCollectionValidationEntityError> Errors)
    : OpenApiCollectionValidationEntity(Errors);

public sealed record OpenApiCollectionValidationModel(
    string Name,
    IReadOnlyList<OpenApiCollectionValidationEntityError> Errors)
    : OpenApiCollectionValidationEntity(Errors);

public sealed record OpenApiCollectionValidationCollection(
    OpenApiCollectionReference? OpenApiCollection,
    IReadOnlyList<OpenApiCollectionValidationEntity> Entities);

public sealed record OpenApiCollectionValidationError(
    IReadOnlyList<OpenApiCollectionValidationCollection> Collections)
    : MutationError("OpenAPI collection validation failed.");

public sealed record McpFeatureCollectionReference(string Id, string Name);

public abstract record McpFeatureCollectionValidationEntityError(string Message);

public sealed record McpFeatureCollectionValidationDocumentError(
    string Message,
    string? Code,
    string? Path,
    IReadOnlyList<MutationErrorLocation>? Locations)
    : McpFeatureCollectionValidationEntityError(Message);

public sealed record McpFeatureCollectionValidationEntityValidationError(string Message)
    : McpFeatureCollectionValidationEntityError(Message);

public abstract record McpFeatureCollectionValidationEntity(
    IReadOnlyList<McpFeatureCollectionValidationEntityError> Errors);

public sealed record McpFeatureCollectionValidationPrompt(
    string Name,
    IReadOnlyList<McpFeatureCollectionValidationEntityError> Errors)
    : McpFeatureCollectionValidationEntity(Errors);

public sealed record McpFeatureCollectionValidationTool(
    string Name,
    IReadOnlyList<McpFeatureCollectionValidationEntityError> Errors)
    : McpFeatureCollectionValidationEntity(Errors);

public sealed record McpFeatureCollectionValidationCollection(
    McpFeatureCollectionReference? McpFeatureCollection,
    IReadOnlyList<McpFeatureCollectionValidationEntity> Entities);

public sealed record McpFeatureCollectionValidationError(
    IReadOnlyList<McpFeatureCollectionValidationCollection> Collections)
    : MutationError("MCP feature collection validation failed.");
