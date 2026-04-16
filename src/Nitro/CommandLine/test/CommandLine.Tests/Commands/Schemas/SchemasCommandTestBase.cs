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

    protected MemoryStream SetupUploadSchemaMutation(
        params IUploadSchema_UploadSchema_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        SchemasClientMock
            .Setup(x => x.UploadSchemaAsync(
                ApiId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateUploadSchemaPayload(errors));

        return capturedStream;
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

    protected MemoryStream SetupSchemaValidationMutation(
        params IValidateSchemaVersion_ValidateSchema_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        SchemasClientMock
            .Setup(x => x.StartSchemaValidationAsync(
                ApiId,
                Stage,
                It.IsAny<Stream>(),
                It.IsAny<SourceMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateValidateSchemaVersionPayload(errors));

        return capturedStream;
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

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishFailedEventWithErrors()
    {
        var errorMock = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        MockErrorFactory.SetupUnexpectedProcessingError(errorMock, "Something went wrong during publish.");

        return CreateSchemaVersionPublishFailedEvent(errorMock.Object);
    }

    protected static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate
        CreateSchemaVersionPublishWaitForApprovalEventWithErrors()
    {
        var deploymentMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment>(
            MockBehavior.Strict);
        MockErrorFactory.SetupSchemaDeployment(deploymentMock);

        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            deploymentMock.Object);
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
        MockErrorFactory.SetupSchemaChangeViolationError(schemaViolation);

        // 2. InvalidGraphQLSchemaError with one error entry
        var graphqlError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_InvalidGraphQLSchemaError>(
                MockBehavior.Strict);
        MockErrorFactory.SetupInvalidGraphQLSchemaError(graphqlError);

        // 3. PersistedQueryValidationError with a client and query
        var pqError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_PersistedQueryValidationError>(
                MockBehavior.Strict);
        MockErrorFactory.SetupPersistedQueryValidationError(pqError);

        // 4. OpenApiCollectionValidationError with an endpoint entity
        var openApiError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_OpenApiCollectionValidationError>(
                MockBehavior.Strict);
        MockErrorFactory.SetupOpenApiCollectionValidationError(openApiError);

        // 5. McpFeatureCollectionValidationError with a tool entity
        var mcpError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_McpFeatureCollectionValidationError>(
                MockBehavior.Strict);
        MockErrorFactory.SetupMcpFeatureCollectionValidationError(mcpError);

        // 6. UnexpectedProcessingError
        var unexpectedError =
            new Mock<
                IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_UnexpectedProcessingError>(
                MockBehavior.Strict);
        MockErrorFactory.SetupUnexpectedProcessingError(unexpectedError);

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
