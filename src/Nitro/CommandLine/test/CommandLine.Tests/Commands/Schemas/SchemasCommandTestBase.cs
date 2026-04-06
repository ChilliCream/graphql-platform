using System.Text;
using ChilliCream.Nitro.Client;
using Moq;
using Moq.Language;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public abstract class SchemasCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string RequestId = "request-id";

    protected const string SchemaFile = "schema.graphql";
    private const string SchemaContent = "type Query { hello: String }";

    protected void SetupSchemaFile()
    {
        SetupFile(SchemaFile, SchemaContent);
    }

    #region Upload

    protected void SetupUploadSchemaMutation(
        params IUploadSchema_UploadSchema_Errors[] errors)
    {
        SchemasClientMock
            .Setup(x => x.UploadSchemaAsync(
                ApiId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateUploadSchemaPayload(errors));
    }

    protected void SetupUploadSchemaMutationException()
    {
        SchemasClientMock
            .Setup(x => x.UploadSchemaAsync(
                ApiId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Publish

    protected void SetupPublishSchemaMutation(
        bool force = false,
        bool waitForApproval = false,
        params IPublishSchemaVersion_PublishSchema_Errors[] errors)
    {
        SchemasClientMock
            .Setup(x => x.StartSchemaPublishAsync(
                ApiId, Stage, Tag, force, waitForApproval, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreatePublishSchemaPayload(errors));
    }

    protected void SetupPublishSchemaMutationException()
    {
        SchemasClientMock
            .Setup(x => x.StartSchemaPublishAsync(
                ApiId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate>>? _schemaPublishSetup;

    protected void SetupPublishSchemaSubscription(
        params IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateSchemaVersionPublishSuccessEvent()];
        }

        SetupSchemaVersionPublishUpdatedSubscription(events);
    }

    private void SetupSchemaVersionPublishUpdatedSubscription(
        IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[] events)
    {
        _schemaPublishSetup ??= SchemasClientMock
            .SetupSequence(x => x.SubscribeToSchemaPublishAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _schemaPublishSetup.Returns(events.ToAsyncEnumerable());
    }

    #endregion

    #region Validate

    protected void SetupSchemaValidationMutation(
        params IValidateSchemaVersion_ValidateSchema_Errors[] errors)
    {
        SchemasClientMock
            .Setup(x => x.StartSchemaValidationAsync(
                ApiId,
                Stage,
                It.IsAny<Stream>(),
                It.IsAny<SourceMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateValidateSchemaVersionPayload(errors));
    }

    protected void SetupSchemaValidationMutationException()
    {
        SchemasClientMock
            .Setup(x => x.StartSchemaValidationAsync(
                ApiId,
                Stage,
                It.IsAny<Stream>(),
                It.IsAny<SourceMetadata>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupSchemaValidationSubscription(
        params IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateSchemaVersionValidationSuccessEvent()];
        }

        SetupSchemaValidationSubscriptionInternal(events);
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate>>? _schemaValidationSetup;

    private void SetupSchemaValidationSubscriptionInternal(
        IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[] events)
    {
        _schemaValidationSetup ??= SchemasClientMock
            .SetupSequence(x => x.SubscribeToSchemaValidationAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _schemaValidationSetup.Returns(events.ToAsyncEnumerable());
    }

    private static IValidateSchemaVersion_ValidateSchema
        CreateValidateSchemaVersionPayload(
            IValidateSchemaVersion_ValidateSchema_Errors[] errors)
    {
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    #endregion

    #region Download

    protected void SetupDownloadSchema()
    {
        SchemasClientMock
            .Setup(x => x.DownloadLatestSchemaAsync(
                ApiId, Stage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(SchemaContent)));
    }

    protected void SetupMissingDownloadSchema()
    {
        SchemasClientMock
            .Setup(x => x.DownloadLatestSchemaAsync(
                ApiId, Stage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);
    }

    protected void SetupDownloadSchemaException()
    {
        SchemasClientMock
            .Setup(x => x.DownloadLatestSchemaAsync(
                ApiId, Stage, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Subscription Event Factories — Schema Version Publish

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishSuccessEvent()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishSuccess(
            "SchemaVersionPublishSuccess",
            ProcessingState.Success);
    }

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishFailedEvent(
            params IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors[] errors)
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishFailed(
            "SchemaVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishOperationInProgressEvent()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishQueuedEvent(int queuePosition)
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishReadyEvent()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishApprovedEvent()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishWaitForApprovalEvent()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);
    }

    #endregion

    #region Subscription Event Factories — Schema Version Validation

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationSuccessEvent()
    {
        return new Mock<
            IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationSuccess>(
            MockBehavior.Strict).Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationFailedEvent(
            params IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors[] errors)
    {
        var mock =
            new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed>(
                MockBehavior.Strict);
        mock.SetupGet(x => x.Errors).Returns(errors);
        return mock.Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionOperationInProgressEvent()
    {
        return new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_OperationInProgress>(
            MockBehavior.Strict).Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationInProgressEvent()
    {
        return new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_ValidationInProgress>(
            MockBehavior.Strict).Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationFailedEventWithErrors()
    {
        // 1. SchemaVersionChangeViolationError (empty changes list)
        var schemaViolation =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_SchemaVersionChangeViolationError>(
                MockBehavior.Strict);
        schemaViolation.SetupGet(x => x.__typename).Returns("SchemaVersionChangeViolationError");
        schemaViolation.SetupGet(x => x.Changes).Returns(Array
            .Empty<
                IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>());

        // 2. InvalidGraphQLSchemaError with one error entry
        var schemaErrorEntry =
            new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors>(
                MockBehavior.Strict);
        schemaErrorEntry.SetupGet(x => x.Message).Returns("Field 'Query.foo' has no type.");
        schemaErrorEntry.SetupGet(x => x.Code).Returns("SCHEMA_ERROR");

        var graphqlError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_InvalidGraphQLSchemaError>(
                MockBehavior.Strict);
        graphqlError.SetupGet(x => x.__typename).Returns("InvalidGraphQLSchemaError");
        graphqlError.SetupGet(x => x.Message).Returns("Invalid GraphQL schema.");
        graphqlError.SetupGet(x => x.Errors).Returns(new[] { schemaErrorEntry.Object });

        // 3. PersistedQueryValidationError with a client and query
        var pqClient =
            new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client>(MockBehavior
                .Strict);
        pqClient.SetupGet(x => x.Name).Returns("test-client");
        pqClient.SetupGet(x => x.Id).Returns("client-1");

        var pqQuery =
            new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(MockBehavior
                .Strict);
        pqQuery.SetupGet(x => x.Hash).Returns("abc123");
        pqQuery.SetupGet(x => x.Message).Returns("Query failed.");
        pqQuery.SetupGet(x => x.DeployedTags).Returns(Array.Empty<string>());
        pqQuery.SetupGet(x => x.Errors).Returns(Array
            .Empty<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>());

        var pqError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_PersistedQueryValidationError>(
                MockBehavior.Strict);
        pqError.SetupGet(x => x.Message).Returns("Persisted query validation failed.");
        pqError.SetupGet(x => x.Client).Returns(pqClient.Object);
        pqError.SetupGet(x => x.Queries).Returns(new[] { pqQuery.Object });

        // 4. OpenApiCollectionValidationError with an endpoint entity
        var openApiLocation =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations_1>(
                MockBehavior.Strict);
        openApiLocation.SetupGet(x => x.Line).Returns(10);
        openApiLocation.SetupGet(x => x.Column).Returns(5);

        var openApiDocError =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_OpenApiCollectionValidationDocumentError>(
                MockBehavior.Strict);
        openApiDocError.SetupGet(x => x.Code).Returns("INVALID");
        openApiDocError.SetupGet(x => x.Message).Returns("Invalid schema.");
        openApiDocError.SetupGet(x => x.Path).Returns("/paths/~1pets");
        openApiDocError.SetupGet(x => x.Locations).Returns(new[] { openApiLocation.Object });

        var openApiEndpoint =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_OpenApiCollectionValidationEndpoint>(
                MockBehavior.Strict);
        openApiEndpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.HttpMethod).Returns("GET");
        openApiEndpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.Route).Returns("/pets");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_1[]
            openApiEntityErrors = [openApiDocError.Object];
        openApiEndpoint.As<IOpenApiCollectionValidationEntity_OpenApiCollectionValidationEndpoint>()
            .SetupGet(x => x.Errors).Returns(openApiEntityErrors);
        openApiEndpoint.As<IOpenApiCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(openApiEntityErrors);

        var openApiCol =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollection>(
                MockBehavior.Strict);
        openApiCol.SetupGet(x => x.Name).Returns("petstore");
        openApiCol.SetupGet(x => x.Id).Returns("collection-1");

        var openApiColValidation =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollectionValidationCollection>(
                MockBehavior.Strict);
        openApiColValidation.SetupGet(x => x.OpenApiCollection).Returns(openApiCol.Object);
        openApiColValidation.SetupGet(x => x.Entities).Returns(new[] { openApiEndpoint.Object });

        var openApiError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_OpenApiCollectionValidationError>(
                MockBehavior.Strict);
        openApiError.SetupGet(x => x.Collections).Returns(new[] { openApiColValidation.Object });

        // 5. McpFeatureCollectionValidationError with a tool entity
        var mcpDocError =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_McpFeatureCollectionValidationDocumentError>(
                MockBehavior.Strict);
        mcpDocError.SetupGet(x => x.Code).Returns("INVALID");
        mcpDocError.SetupGet(x => x.Message).Returns("Invalid MCP schema.");
        mcpDocError.SetupGet(x => x.Path).Returns("/tools/test");
        var mcpLocation =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations>(
                MockBehavior.Strict);
        mcpLocation.SetupGet(x => x.Line).Returns(5);
        mcpLocation.SetupGet(x => x.Column).Returns(3);
        mcpDocError.SetupGet(x => x.Locations).Returns(new[] { mcpLocation.Object });

        var mcpTool =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_McpFeatureCollectionValidationTool>(
                MockBehavior.Strict);
        mcpTool.As<IMcpFeatureCollectionValidationTool>().SetupGet(x => x.Name).Returns("test-tool");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors[]
            mcpEntityErrors = [mcpDocError.Object];
        mcpTool.As<IMcpFeatureCollectionValidationEntity_McpFeatureCollectionValidationTool>().SetupGet(x => x.Errors)
            .Returns(mcpEntityErrors);
        mcpTool.As<IMcpFeatureCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(mcpEntityErrors);

        var mcpCol =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollection>(
                MockBehavior.Strict);
        mcpCol.SetupGet(x => x.Name).Returns("mcp-collection");
        mcpCol.SetupGet(x => x.Id).Returns("mcp-1");

        var mcpColValidation =
            new Mock<
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollectionValidationCollection>(
                MockBehavior.Strict);
        mcpColValidation.SetupGet(x => x.McpFeatureCollection).Returns(mcpCol.Object);
        mcpColValidation.SetupGet(x => x.Entities).Returns(new[] { mcpTool.Object });

        var mcpError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_McpFeatureCollectionValidationError>(
                MockBehavior.Strict);
        mcpError.SetupGet(x => x.Collections).Returns(new[] { mcpColValidation.Object });

        // 6. UnexpectedProcessingError
        var unexpectedError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_UnexpectedProcessingError>(
                MockBehavior.Strict);
        unexpectedError.SetupGet(x => x.__typename).Returns("UnexpectedProcessingError");
        unexpectedError.SetupGet(x => x.Message).Returns("An unexpected error occurred.");

        // Assemble the event with all errors
        var mock =
            new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed>(
                MockBehavior.Strict);
        mock.SetupGet(x => x.Errors)
            .Returns(new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors[]
            {
                schemaViolation.Object, graphqlError.Object, pqError.Object, openApiError.Object, mcpError.Object,
                unexpectedError.Object
            });
        return mock.Object;
    }

    #endregion

    #region Error Factories — UploadSchema

    protected static IUploadSchema_UploadSchema_Errors
        CreateUploadSchemaUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IUploadSchema_UploadSchema_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IUploadSchema_UploadSchema_Errors
        CreateUploadSchemaApiNotFoundError(string apiId = ApiId)
    {
        var mock = new Mock<IUploadSchema_UploadSchema_Errors_ApiNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"API '{apiId}' was not found.");
        mock.SetupGet(x => x.ApiId).Returns(apiId);
        return mock.Object;
    }

    protected static IUploadSchema_UploadSchema_Errors
        CreateUploadSchemaDuplicatedTagError(string tag = Tag)
    {
        var mock = new Mock<IUploadSchema_UploadSchema_Errors_DuplicatedTagError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Tag '{tag}' already exists.");
        return mock.Object;
    }

    protected static IUploadSchema_UploadSchema_Errors
        CreateUploadSchemaConcurrentOperationError()
    {
        var mock = new Mock<IUploadSchema_UploadSchema_Errors_ConcurrentOperationError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns("A concurrent operation is in progress.");
        return mock.Object;
    }

    #endregion

    #region Error Factories — PublishSchemaVersion

    protected static IPublishSchemaVersion_PublishSchema_Errors
        CreatePublishSchemaUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IPublishSchemaVersion_PublishSchema_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IPublishSchemaVersion_PublishSchema_Errors
        CreatePublishSchemaApiNotFoundError(string apiId = ApiId)
    {
        var mock = new Mock<IPublishSchemaVersion_PublishSchema_Errors_ApiNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"API '{apiId}' was not found.");
        mock.SetupGet(x => x.ApiId).Returns(apiId);
        return mock.Object;
    }

    protected static IPublishSchemaVersion_PublishSchema_Errors
        CreatePublishSchemaStageNotFoundError(string name = Stage)
    {
        var mock = new Mock<IPublishSchemaVersion_PublishSchema_Errors_StageNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Stage '{name}' was not found.");
        mock.SetupGet(x => x.Name).Returns(name);
        return mock.Object;
    }

    protected static IPublishSchemaVersion_PublishSchema_Errors
        CreatePublishSchemaSchemaNotFoundError()
    {
        var mock = new Mock<IPublishSchemaVersion_PublishSchema_Errors_SchemaNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns("Schema not found.");
        return mock.Object;
    }

    #endregion

    #region Error Factories — ValidateSchemaVersion

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionApiNotFoundError(string apiId = ApiId)
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_ApiNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"API '{apiId}' was not found.");
        mock.SetupGet(x => x.ApiId).Returns(apiId);
        return mock.Object;
    }

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionStageNotFoundError(string name = Stage)
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_StageNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Stage '{name}' was not found.");
        mock.SetupGet(x => x.Name).Returns(name);
        return mock.Object;
    }

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionSchemaNotFoundError(string message = "Schema not found.")
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_SchemaNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    #region Payload Factories

    private static IUploadSchema_UploadSchema CreateUploadSchemaPayload(
        IUploadSchema_UploadSchema_Errors[] errors)
    {
        var payload = new Mock<IUploadSchema_UploadSchema>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.Errors).Returns(errors);
            payload.SetupGet(x => x.SchemaVersion)
                .Returns((IUploadSchema_UploadSchema_SchemaVersion?)null);
        }
        else
        {
            var schemaVersion = new Mock<IUploadSchema_UploadSchema_SchemaVersion>(MockBehavior.Strict);
            schemaVersion.SetupGet(x => x.Id).Returns("sv-1");

            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<IUploadSchema_UploadSchema_Errors>?)null);
            payload.SetupGet(x => x.SchemaVersion).Returns(schemaVersion.Object);
        }

        return payload.Object;
    }

    private static IPublishSchemaVersion_PublishSchema CreatePublishSchemaPayload(
        IPublishSchemaVersion_PublishSchema_Errors[] errors)
    {
        var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    #endregion
}
