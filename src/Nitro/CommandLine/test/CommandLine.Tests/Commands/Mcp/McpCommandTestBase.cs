using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using HotChocolate.Adapters.Mcp.Packaging;
using Moq;
using Moq.Language;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public abstract class McpCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string McpFeatureCollectionId = "mcp-1";
    protected const string McpFeatureCollectionName = "my-mcp";
    protected const string RequestId = "request-1";

    protected void SetupMcpDefinitionFiles()
    {
        SetupGlobMatch(["prompt.mcp-prompt.json"]);
        SetupOpenReadStream("prompt.mcp-prompt.json", "{}"u8.ToArray());
    }

    protected static async Task AssertMcpFeatureCollectionArchive(Stream stream)
    {
        using var archive = McpFeatureCollectionArchive.Open(stream, leaveOpen: true);

        var prompt = await archive.TryGetPromptAsync("prompt.mcp-prompt");
        Assert.NotNull(prompt);
        Assert.Equal("{}", prompt.Settings.RootElement.ToString());
    }

    protected void SetupEmptyMcpDefinitionFiles()
    {
        SetupGlobMatch([]);
    }

    #region Create

    protected void SetupCreateMcpFeatureCollectionMutation(
        params ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors[] errors)
    {
        McpClientMock.Setup(x => x.CreateMcpFeatureCollectionAsync(
                ApiId, McpFeatureCollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreateMcpFeatureCollectionPayload(errors));
    }

    protected void SetupCreateMcpFeatureCollectionMutationException()
    {
        McpClientMock.Setup(x => x.CreateMcpFeatureCollectionAsync(
                ApiId, McpFeatureCollectionName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreateMcpFeatureCollectionMutationNullResult()
    {
        McpClientMock.Setup(x => x.CreateMcpFeatureCollectionAsync(
                ApiId, McpFeatureCollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreateMcpFeatureCollectionPayloadNull());
    }

    #endregion

    #region Delete

    protected void SetupDeleteMcpFeatureCollectionMutation(
        params IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors[] errors)
    {
        McpClientMock.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                McpFeatureCollectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteMcpFeatureCollectionPayload(errors));
    }

    protected void SetupDeleteMcpFeatureCollectionMutationException()
    {
        McpClientMock.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                McpFeatureCollectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupDeleteMcpFeatureCollectionMutationNullResult()
    {
        McpClientMock.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                McpFeatureCollectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteMcpFeatureCollectionPayloadNull());
    }

    #endregion

    #region List

    protected void SetupListMcpFeatureCollectionsQuery(
        string? cursor = null,
        int first = 10,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name)[] items)
    {
        var nodes = items
            .Select(static item =>
                (IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node)
                new ListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node_McpFeatureCollection(
                    item.Id,
                    item.Name))
            .ToArray();

        McpClientMock.Setup(x => x.ListMcpFeatureCollectionsAsync(
                ApiId, cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node>(
                nodes, endCursor, hasNextPage));
    }

    protected void SetupListMcpFeatureCollectionsQueryException()
    {
        McpClientMock.Setup(x => x.ListMcpFeatureCollectionsAsync(
                ApiId, null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Upload

    protected MemoryStream SetupUploadMcpFeatureCollectionMutation(
        params IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        McpClientMock.Setup(x => x.UploadMcpFeatureCollectionVersionAsync(
                McpFeatureCollectionId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateUploadMcpFeatureCollectionPayload(errors));

        return capturedStream;
    }

    protected void SetupUploadMcpFeatureCollectionMutationException()
    {
        McpClientMock.Setup(x => x.UploadMcpFeatureCollectionVersionAsync(
                McpFeatureCollectionId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupUploadMcpFeatureCollectionMutationNullVersion()
    {
        var payload = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.McpFeatureCollectionVersion)
            .Returns((IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_McpFeatureCollectionVersion?)null);

        McpClientMock.Setup(x => x.UploadMcpFeatureCollectionVersionAsync(
                McpFeatureCollectionId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    #endregion

    #region Validate

    protected MemoryStream SetupValidateMcpFeatureCollectionMutation(
        params IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        McpClientMock.Setup(x => x.StartMcpFeatureCollectionValidationAsync(
                McpFeatureCollectionId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateValidateMcpFeatureCollectionPayload(errors));

        return capturedStream;
    }

    protected void SetupValidateMcpFeatureCollectionMutationException()
    {
        McpClientMock.Setup(x => x.StartMcpFeatureCollectionValidationAsync(
                McpFeatureCollectionId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupValidateMcpFeatureCollectionMutationNullRequestId()
    {
        var payload = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        McpClientMock.Setup(x => x.StartMcpFeatureCollectionValidationAsync(
                McpFeatureCollectionId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate>>? _mcpValidationSetup;

    protected void SetupValidateMcpFeatureCollectionSubscription(
        params IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateMcpFeatureCollectionValidationSuccessEvent()];
        }

        _mcpValidationSetup ??= McpClientMock
            .SetupSequence(x => x.SubscribeToMcpFeatureCollectionValidationAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _mcpValidationSetup.Returns(events.ToAsyncEnumerable());
    }

    #endregion

    #region Publish

    protected void SetupPublishMcpFeatureCollectionMutation(
        bool force = false,
        bool waitForApproval = false,
        params IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors[] errors)
    {
        McpClientMock.Setup(x => x.StartMcpFeatureCollectionPublishAsync(
                McpFeatureCollectionId, Stage, Tag, force, waitForApproval, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreatePublishMcpFeatureCollectionPayload(errors));
    }

    protected void SetupPublishMcpFeatureCollectionMutationException()
    {
        McpClientMock.Setup(x => x.StartMcpFeatureCollectionPublishAsync(
                McpFeatureCollectionId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupPublishMcpFeatureCollectionMutationNullRequestId()
    {
        var payload = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        McpClientMock.Setup(x => x.StartMcpFeatureCollectionPublishAsync(
                McpFeatureCollectionId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate>>? _mcpPublishSetup;

    protected void SetupPublishMcpFeatureCollectionSubscription(
        params IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateMcpFeatureCollectionPublishSuccessEvent()];
        }

        _mcpPublishSetup ??= McpClientMock
            .SetupSequence(x => x.SubscribeToMcpFeatureCollectionPublishAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _mcpPublishSetup.Returns(events.ToAsyncEnumerable());
    }

    #endregion

    #region Subscription Event Factories -- MCP Feature Collection Publish

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishSuccessEvent()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_McpFeatureCollectionVersionPublishSuccess(
            "McpFeatureCollectionVersionPublishSuccess",
            ProcessingState.Success);
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishFailedEvent(
            params IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors[] errors)
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_McpFeatureCollectionVersionPublishFailed(
            "McpFeatureCollectionVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishOperationInProgressEvent()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishQueuedEvent(int queuePosition)
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishReadyEvent()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishApprovedEvent()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishWaitForApprovalEvent()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishFailedEventWithErrors()
    {
        var errorMock = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        return CreateMcpFeatureCollectionPublishFailedEvent(errorMock.Object);
    }

    protected static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate
        CreateMcpFeatureCollectionPublishWaitForApprovalEventWithErrors()
    {
        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_2>(
            MockBehavior.Strict);
        errorMock.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(Array.Empty<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections>());

        var deploymentMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment>(
            MockBehavior.Strict);
        deploymentMock.As<IMcpFeatureCollectionDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { errorMock.Object });

        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            deploymentMock.Object);
    }

    #endregion

    #region Subscription Event Factories -- MCP Feature Collection Validation

    protected static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate
        CreateMcpFeatureCollectionValidationSuccessEvent()
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_McpFeatureCollectionVersionValidationSuccess(
            "McpFeatureCollectionVersionValidationSuccess",
            ProcessingState.Success);
    }

    protected static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate
        CreateMcpFeatureCollectionValidationFailedEvent(
            params IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors[] errors)
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_McpFeatureCollectionVersionValidationFailed(
            "McpFeatureCollectionVersionValidationFailed",
            ProcessingState.Failed,
            errors);
    }

    protected static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate
        CreateMcpFeatureCollectionValidationOperationInProgressEvent()
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    protected static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate
        CreateMcpFeatureCollectionValidationInProgressEvent()
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    protected static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate
        CreateMcpFeatureCollectionValidationFailedEventWithErrors()
    {
        var location = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations>(
            MockBehavior.Strict);
        location.SetupGet(x => x.Line).Returns(5);
        location.SetupGet(x => x.Column).Returns(3);

        var docError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_McpFeatureCollectionValidationDocumentError>(
            MockBehavior.Strict);
        docError.SetupGet(x => x.Code).Returns("INVALID");
        docError.SetupGet(x => x.Message).Returns("Invalid tool definition.");
        docError.SetupGet(x => x.Path).Returns("/tools/test");
        docError.SetupGet(x => x.Locations).Returns(new[] { location.Object });

        var tool = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_McpFeatureCollectionValidationTool>(
            MockBehavior.Strict);
        tool.As<IMcpFeatureCollectionValidationTool>().SetupGet(x => x.Name).Returns("test-tool");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors[]
            entityErrors = [docError.Object];
        tool.As<IMcpFeatureCollectionValidationEntity_McpFeatureCollectionValidationTool>()
            .SetupGet(x => x.Errors).Returns(entityErrors);
        tool.As<IMcpFeatureCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(entityErrors);

        var mcpCol = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollection>(
            MockBehavior.Strict);
        mcpCol.SetupGet(x => x.Name).Returns("mcp-collection");
        mcpCol.SetupGet(x => x.Id).Returns("mcp-1");

        var colValidation = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollectionValidationCollection>(
            MockBehavior.Strict);
        colValidation.SetupGet(x => x.McpFeatureCollection).Returns(mcpCol.Object);
        colValidation.SetupGet(x => x.Entities)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities[]
                { tool.Object });

        var errorMock = new Mock<
            IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections[]
                { colValidation.Object });

        return CreateMcpFeatureCollectionValidationFailedEvent(errorMock.Object);
    }

    #endregion

    #region Payload Factories

    private static ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection CreateCreateMcpFeatureCollectionPayload(
        ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.McpFeatureCollection)
                .Returns((ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_McpFeatureCollection?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            payload.SetupGet(x => x.McpFeatureCollection)
                .Returns(new CreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_McpFeatureCollection_McpFeatureCollection(
                    McpFeatureCollectionName, McpFeatureCollectionId));
            payload.SetupGet(x => x.Errors)
                .Returns(Array.Empty<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors>());
        }

        return payload.Object;
    }

    private static ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection CreateCreateMcpFeatureCollectionPayloadNull()
    {
        var payload = new Mock<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns((ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_McpFeatureCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors>());
        return payload.Object;
    }

    private static IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById CreateDeleteMcpFeatureCollectionPayload(
        IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors[] errors)
    {
        var payload = new Mock<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.McpFeatureCollection)
                .Returns((IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_McpFeatureCollection?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            payload.SetupGet(x => x.McpFeatureCollection)
                .Returns(new DeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_McpFeatureCollection_McpFeatureCollection(
                    McpFeatureCollectionName, McpFeatureCollectionId));
            payload.SetupGet(x => x.Errors)
                .Returns(Array.Empty<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors>());
        }

        return payload.Object;
    }

    private static IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById CreateDeleteMcpFeatureCollectionPayloadNull()
    {
        var payload = new Mock<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns((IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_McpFeatureCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors>());
        return payload.Object;
    }

    private static IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection CreateUploadMcpFeatureCollectionPayload(
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.Errors).Returns(errors);
            payload.SetupGet(x => x.McpFeatureCollectionVersion)
                .Returns((IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_McpFeatureCollectionVersion?)null);
        }
        else
        {
            var version = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_McpFeatureCollectionVersion>(MockBehavior.Strict);
            version.SetupGet(x => x.Id).Returns("mcpv-1");

            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors>?)null);
            payload.SetupGet(x => x.McpFeatureCollectionVersion).Returns(version.Object);
        }

        return payload.Object;
    }

    private static IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection CreateValidateMcpFeatureCollectionPayload(
        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection CreatePublishMcpFeatureCollectionPayload(
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    #endregion
}
