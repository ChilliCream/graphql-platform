using StrawberryShake;

namespace ChilliCream.Nitro.Client;

internal static class MutationErrorMapper
{
    public static IReadOnlyList<MutationError> MapMany<TError>(IEnumerable<TError>? errors)
    {
        if (errors is null)
        {
            return [];
        }

        var mapped = errors
            .Select(static error => Map(error))
            .Where(static error => error is not null)
            .Select(static error => error!)
            .ToArray();

        return mapped.Length > 0
            ? mapped
            : [];
    }

    private static MutationError? Map(object? error)
    {
        if (error is null)
        {
            return null;
        }

        return error switch
        {
            IOperationsAreNotAllowedError e => new OperationsAreNotAllowedError(e.Message),
            IConcurrentOperationError e => new ConcurrentOperationError(e.Message),
            IUnexpectedProcessingError e => new UnexpectedProcessingError(e.Message),
            IProcessingTimeoutError e => new ProcessingTimeoutError(e.Message),
            ISchemaVersionSyntaxError e => new SchemaVersionSyntaxError(
                e.Message,
                e.Line,
                e.Column,
                e.Position),
            ISchemaVersionChangeViolationError e => new SchemaVersionChangeViolationError(
                MapSchemaChanges(e.Changes)),
            ISchemaChangeViolationError e => new SchemaChangeViolationError(
                e.Message,
                MapSchemaChanges(e.Changes)),
            IInvalidGraphQLSchemaError e => new InvalidGraphQLSchemaError(
                e.Message,
                MapGraphQlSchemaErrors(e.Errors)),
            IPersistedQueryValidationError e => MapPersistedQueryValidationError(e),
            IOpenApiCollectionValidationError e => MapOpenApiCollectionValidationError(e),
            IInvalidOpenApiCollectionArchiveError e => new InvalidOpenApiCollectionArchiveError(e.Message),
            IOpenApiCollectionValidationArchiveError e => new OpenApiCollectionValidationArchiveError(e.Message),
            IMcpFeatureCollectionValidationError e => MapMcpFeatureCollectionValidationError(e),
            IInvalidMcpFeatureCollectionArchiveError e => new InvalidMcpFeatureCollectionArchiveError(e.Message),
            IMcpFeatureCollectionValidationArchiveError e => new McpFeatureCollectionValidationArchiveError(e.Message),
            IApiNotFoundError e => new ApiNotFoundError(e.Message, e.ApiId),
            IMockSchemaNonUniqueNameError e => new MockSchemaNonUniqueNameError(e.Message, e.Name),
            IMockSchemaNotFoundError e => new MockSchemaNotFoundError(e.Message),
            IStageNotFoundError e => new StageNotFoundError(e.Message, e.Name),
            ISubgraphInvalidError e => new SubgraphInvalidError(e.Message),
            IInvalidFusionSourceSchemaArchiveError e => new InvalidFusionSourceSchemaArchiveError(e.Message),
            IStagesHavePublishedDependenciesError e => MapStagesHavePublishedDependenciesError(e),
            IError e => new UnknownMutationError(e.Message),
            _ when error.GetType().Name.Contains("ReadyTimeoutError", StringComparison.Ordinal)
                => new ReadyTimeoutError(
                    "Validation timed out while waiting for processing to become ready."),
            _ => new UnknownMutationError("The server returned an unknown mutation error.")
        };
    }

    private static PersistedQueryValidationError MapPersistedQueryValidationError(
        IPersistedQueryValidationError error)
    {
        var client = error.Client is null
            ? null
            : new PersistedQueryValidationClient(error.Client.Id, error.Client.Name);

        var queries = error.Queries
            .Select(static query => new PersistedQueryValidationQuery(
                query.Message,
                query.Hash,
                query.DeployedTags.ToArray(),
                query.Errors
                    .Select(static queryError => new PersistedQueryValidationIssue(
                        queryError.Message,
                        queryError.Code,
                        queryError.Path,
                        MapLocations(queryError.Locations)))
                    .ToArray()))
            .ToArray();

        return new PersistedQueryValidationError(
            error.Message,
            client,
            queries);
    }

    private static OpenApiCollectionValidationError MapOpenApiCollectionValidationError(
        IOpenApiCollectionValidationError error)
    {
        var collections = error.Collections
            .Select(MapOpenApiCollectionValidationCollection)
            .ToArray();

        return new OpenApiCollectionValidationError(collections);
    }

    private static OpenApiCollectionValidationCollection MapOpenApiCollectionValidationCollection(
        IOpenApiCollectionValidationCollection collection)
    {
        var openApiCollection = collection.OpenApiCollection is null
            ? null
            : new OpenApiCollectionReference(
                collection.OpenApiCollection.Id,
                collection.OpenApiCollection.Name);

        var entities = collection.Entities
            .Select(MapOpenApiCollectionValidationEntity)
            .ToArray();

        return new OpenApiCollectionValidationCollection(openApiCollection, entities);
    }

    private static OpenApiCollectionValidationEntity MapOpenApiCollectionValidationEntity(
        IOpenApiCollectionValidationEntity entity)
    {
        var entityErrors = entity.Errors
            .Select(MapOpenApiCollectionValidationEntityError)
            .ToArray();

        return entity switch
        {
            IOpenApiCollectionValidationEntity_OpenApiCollectionValidationEndpoint endpoint
                => new OpenApiCollectionValidationEndpoint(
                    endpoint.HttpMethod,
                    endpoint.Route,
                    entityErrors),
            IOpenApiCollectionValidationEntity_OpenApiCollectionValidationModel model
                => new OpenApiCollectionValidationModel(
                    model.Name,
                    entityErrors),
            _ => new OpenApiCollectionValidationModel("Unknown entity", entityErrors)
        };
    }

    private static OpenApiCollectionValidationEntityError MapOpenApiCollectionValidationEntityError(
        object error)
        => error switch
        {
            IOpenApiCollectionValidationDocumentError documentError
                => new OpenApiCollectionValidationDocumentError(
                    documentError.Message,
                    documentError.Code,
                    documentError.Path,
                    MapLocations(documentError.Locations)),
            IOpenApiCollectionValidationEntityValidationError entityValidationError
                => new OpenApiCollectionValidationEntityValidationError(entityValidationError.Message),
            IError typedError
                => new OpenApiCollectionValidationEntityValidationError(typedError.Message),
            _ => new OpenApiCollectionValidationEntityValidationError("Unknown OpenAPI validation error.")
        };

    private static McpFeatureCollectionValidationError MapMcpFeatureCollectionValidationError(
        IMcpFeatureCollectionValidationError error)
    {
        var collections = error.Collections
            .Select(MapMcpFeatureCollectionValidationCollection)
            .ToArray();

        return new McpFeatureCollectionValidationError(collections);
    }

    private static McpFeatureCollectionValidationCollection MapMcpFeatureCollectionValidationCollection(
        IMcpFeatureCollectionValidationCollection collection)
    {
        var mcpFeatureCollection = collection.McpFeatureCollection is null
            ? null
            : new McpFeatureCollectionReference(
                collection.McpFeatureCollection.Id,
                collection.McpFeatureCollection.Name);

        var entities = collection.Entities
            .Select(MapMcpFeatureCollectionValidationEntity)
            .ToArray();

        return new McpFeatureCollectionValidationCollection(mcpFeatureCollection, entities);
    }

    private static McpFeatureCollectionValidationEntity MapMcpFeatureCollectionValidationEntity(
        IMcpFeatureCollectionValidationEntity entity)
    {
        var entityErrors = entity.Errors
            .Select(MapMcpFeatureCollectionValidationEntityError)
            .ToArray();

        return entity switch
        {
            IMcpFeatureCollectionValidationEntity_McpFeatureCollectionValidationPrompt prompt
                => new McpFeatureCollectionValidationPrompt(
                    prompt.Name,
                    entityErrors),
            IMcpFeatureCollectionValidationEntity_McpFeatureCollectionValidationTool tool
                => new McpFeatureCollectionValidationTool(
                    tool.Name,
                    entityErrors),
            _ => new McpFeatureCollectionValidationTool("Unknown entity", entityErrors)
        };
    }

    private static McpFeatureCollectionValidationEntityError MapMcpFeatureCollectionValidationEntityError(
        object error)
        => error switch
        {
            IMcpFeatureCollectionValidationDocumentError documentError
                => new McpFeatureCollectionValidationDocumentError(
                    documentError.Message,
                    documentError.Code,
                    documentError.Path,
                    MapLocations(documentError.Locations)),
            IMcpFeatureCollectionValidationEntityValidationError entityValidationError
                => new McpFeatureCollectionValidationEntityValidationError(entityValidationError.Message),
            IError typedError
                => new McpFeatureCollectionValidationEntityValidationError(typedError.Message),
            _ => new McpFeatureCollectionValidationEntityValidationError("Unknown MCP validation error.")
        };

    private static StagesHavePublishedDependenciesError MapStagesHavePublishedDependenciesError(
        IStagesHavePublishedDependenciesError error)
    {
        var stages = error.Stages
            .Select(static stage => new StageDependency(
                stage.Name,
                stage.PublishedSchema?.Version?.Tag is { } tag
                    ? new PublishedSchemaDependency(tag)
                    : null,
                stage.PublishedClients
                    .Select(static publishedClient => new PublishedClientDependency(
                        new PublishedClientReference(publishedClient.Client.Name),
                        publishedClient.PublishedVersions
                            .Select(static publishedVersion => new PublishedClientVersionDependency(
                                publishedVersion.Version?.Tag ?? "-"))
                            .ToArray()))
                    .ToArray()))
            .ToArray();

        return new StagesHavePublishedDependenciesError(error.Message, stages);
    }

    private static IReadOnlyList<GraphQLSchemaError> MapGraphQlSchemaErrors(
        IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors> errors)
    {
        return errors
            .Select(static error => new GraphQLSchemaError(
                error.Message,
                error.Code))
            .ToArray();
    }

    private static IReadOnlyList<SchemaChangeError> MapSchemaChanges<TChange>(
        IEnumerable<TChange>? changes)
    {
        if (changes is null)
        {
            return [];
        }

        return changes
            .OfType<ISchemaChange>()
            .Select(MapSchemaChange)
            .ToArray();
    }

    private static SchemaChangeError MapSchemaChange(ISchemaChange change)
    {
        var severity = MapSeverity(change.Severity);

        return change switch
        {
            IArgumentAdded c => new SchemaChangeError(
                severity,
                $"The argument {c.Coordinate} was added"),
            IArgumentChanged c => new SchemaChangeError(
                severity,
                $"The argument {c.Coordinate} has changed",
                MapSchemaChanges(c.Changes)),
            IArgumentRemoved c => new SchemaChangeError(
                severity,
                $"The argument {c.Coordinate} was removed"),
            IDeprecatedChange { DeprecationReason: { } reason } => new SchemaChangeError(
                severity,
                $"The member was deprecated with the reason {reason}"),
            IDeprecatedChange => new SchemaChangeError(
                severity,
                "The member was deprecated"),
            IDescriptionChanged { Old: { } oldDescription, New: { } newDescription } => new SchemaChangeError(
                severity,
                $"Description changed from \"{oldDescription}\" to \"{newDescription}\""),
            IDescriptionChanged { New: { } newDescription } => new SchemaChangeError(
                severity,
                $"Description added: \"{newDescription}\""),
            IDescriptionChanged { Old: { } oldDescription } => new SchemaChangeError(
                severity,
                $"Description removed: \"{oldDescription}\""),
            IDirectiveLocationAdded c => new SchemaChangeError(
                severity,
                $"Directive location {c.Location} added"),
            IDirectiveLocationRemoved c => new SchemaChangeError(
                severity,
                $"Directive location {c.Location} removed"),
            IDirectiveModifiedChange c => new SchemaChangeError(
                severity,
                $"Directive {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IEnumModifiedChange c => new SchemaChangeError(
                severity,
                $"Enum {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IEnumValueAdded c => new SchemaChangeError(
                severity,
                $"Enum value {c.Coordinate} was added"),
            IEnumValueChanged c => new SchemaChangeError(
                severity,
                $"Enum value {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IEnumValueRemoved c => new SchemaChangeError(
                severity,
                $"Enum value {c.Coordinate} was removed"),
            IFieldAddedChange c => new SchemaChangeError(
                severity,
                $"Field {c.Coordinate} of type {c.TypeName} was added"),
            IFieldRemovedChange c => new SchemaChangeError(
                severity,
                $"Field {c.Coordinate} of type {c.TypeName} was removed"),
            IInputFieldChanged c => new SchemaChangeError(
                severity,
                $"Field {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IInputObjectModifiedChange c => new SchemaChangeError(
                severity,
                $"Input object {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IInterfaceImplementationAdded c => new SchemaChangeError(
                severity,
                $"Interface implementation {c.InterfaceName} was added"),
            IInterfaceImplementationRemoved c => new SchemaChangeError(
                severity,
                $"Interface implementation {c.InterfaceName} was removed"),
            IInterfaceModifiedChange c => new SchemaChangeError(
                severity,
                $"Interface type {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IObjectModifiedChange c => new SchemaChangeError(
                severity,
                $"Object type {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IOutputFieldChanged c => new SchemaChangeError(
                severity,
                $"Field {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            IPossibleTypeAdded c => new SchemaChangeError(
                severity,
                $"Possible type {c.TypeName} added."),
            IPossibleTypeRemoved c => new SchemaChangeError(
                severity,
                $"Possible type {c.TypeName} removed."),
            IScalarModifiedChange c => new SchemaChangeError(
                severity,
                $"Scalar {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            ITypeChanged c => new SchemaChangeError(
                severity,
                $"Type changed from {c.OldType} to {c.NewType}"),
            ITypeSystemMemberAddedChange c => new SchemaChangeError(
                severity,
                $"Type system member {c.Coordinate} was added."),
            ITypeSystemMemberRemovedChange c => new SchemaChangeError(
                severity,
                $"Type system member {c.Coordinate} was removed."),
            IUnionMemberAdded c => new SchemaChangeError(
                severity,
                $"Type {c.TypeName} was added to the union."),
            IUnionMemberRemoved c => new SchemaChangeError(
                severity,
                $"Type {c.TypeName} was removed from the union."),
            IUnionModifiedChange c => new SchemaChangeError(
                severity,
                $"Union {c.Coordinate} was modified",
                MapSchemaChanges(c.Changes)),
            _ => new SchemaChangeError(
                severity,
                "Unknown schema change. Upgrade to the latest Nitro CLI version.")
        };
    }

    private static SchemaChangeSeverityKind MapSeverity(SchemaChangeSeverity severity)
        => severity switch
        {
            SchemaChangeSeverity.Safe => SchemaChangeSeverityKind.Safe,
            SchemaChangeSeverity.Dangerous => SchemaChangeSeverityKind.Dangerous,
            SchemaChangeSeverity.Breaking => SchemaChangeSeverityKind.Breaking,
            _ => SchemaChangeSeverityKind.Dangerous
        };

    private static IReadOnlyList<MutationErrorLocation>? MapLocations(
        IReadOnlyList<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors_Locations>? locations)
    {
        if (locations is null)
        {
            return null;
        }

        return locations
            .Select(static location => new MutationErrorLocation(location.Line, location.Column))
            .ToArray();
    }

    private static IReadOnlyList<MutationErrorLocation>? MapLocations(
        IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations>? locations)
    {
        if (locations is null)
        {
            return null;
        }

        return locations
            .Select(static location => new MutationErrorLocation(location.Line, location.Column))
            .ToArray();
    }

    private static IReadOnlyList<MutationErrorLocation>? MapLocations(
        IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations_1>? locations)
    {
        if (locations is null)
        {
            return null;
        }

        return locations
            .Select(static location => new MutationErrorLocation(location.Line, location.Column))
            .ToArray();
    }
}
