using System.Text;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using HotChocolate.Adapters.OpenApi.Packaging;
using Moq;
using Moq.Language;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public abstract class OpenApiCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string OpenApiCollectionId = "oa-1";
    protected const string OpenApiCollectionName = "my-openapi";
    protected const string RequestId = "request-1";

    private const string _openApiDocumentContent =
        """query GetUsers @http(method: GET, route: "/users") { users { id } }""";
    private const string _invalidOpenApiDocumentContent =
        """query GetUsers { users { id } }""";

    protected static async Task AssertOpenApiCollectionArchive(Stream stream)
    {
        using var archive = OpenApiCollectionArchive.Open(stream, leaveOpen: true);

        var endpoint = await archive.TryGetOpenApiEndpointAsync(new OpenApiEndpointKey("GET", "/users"));
        Assert.NotNull(endpoint);

        var document = Encoding.UTF8.GetString(endpoint.Document.Span);
        var settings = endpoint.Settings.RootElement.ToString();

        Assert.Equal(
            """
            query GetUsers {
              users {
                id
              }
            }
            """, document);
        Assert.Equal(
            """
            {
              "routeParameters": [],
              "queryParameters": []
            }
            """, settings);
    }

    protected void SetupOpenApiDocument()
    {
        SetupGlobMatch(["document.graphql"]);
        SetupFile("document.graphql", _openApiDocumentContent);
    }

    protected void SetupInvalidOpenApiDocument()
    {
        SetupGlobMatch(["document.graphql"]);
        SetupFile("document.graphql", _invalidOpenApiDocumentContent);
    }

    protected void SetupEmptyGlobMatch()
    {
        SetupGlobMatch([]);
    }

    #region Create

    protected void SetupCreateOpenApiCollectionMutation(
        params ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors[] errors)
    {
        OpenApiClientMock.Setup(x => x.CreateOpenApiCollectionAsync(
                ApiId, OpenApiCollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreateOpenApiCollectionPayload(errors));
    }

    protected void SetupCreateOpenApiCollectionMutationException()
    {
        OpenApiClientMock.Setup(x => x.CreateOpenApiCollectionAsync(
                ApiId, OpenApiCollectionName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreateOpenApiCollectionMutationNullResult()
    {
        var payload = new Mock<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns((ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_OpenApiCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors>());

        OpenApiClientMock.Setup(x => x.CreateOpenApiCollectionAsync(
                ApiId, OpenApiCollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    #endregion

    #region Delete

    protected void SetupDeleteOpenApiCollectionMutation(
        params IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors[] errors)
    {
        OpenApiClientMock.Setup(x => x.DeleteOpenApiCollectionAsync(
                OpenApiCollectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteOpenApiCollectionPayload(errors));
    }

    protected void SetupDeleteOpenApiCollectionMutationException()
    {
        OpenApiClientMock.Setup(x => x.DeleteOpenApiCollectionAsync(
                OpenApiCollectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupDeleteOpenApiCollectionMutationNullResult()
    {
        var payload = new Mock<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns((IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_OpenApiCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors>());

        OpenApiClientMock.Setup(x => x.DeleteOpenApiCollectionAsync(
                OpenApiCollectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    #endregion

    #region List

    protected void SetupListOpenApiCollectionsQuery(
        string? cursor = null,
        int first = 10,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name)[] items)
    {
        var nodes = items
            .Select(static item =>
                (IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node)
                new ListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node_OpenApiCollection(
                    item.Id,
                    item.Name))
            .ToArray();

        OpenApiClientMock.Setup(x => x.ListOpenApiCollectionsAsync(
                ApiId, cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node>(
                nodes, endCursor, hasNextPage));
    }

    protected void SetupListOpenApiCollectionsQueryException()
    {
        OpenApiClientMock.Setup(x => x.ListOpenApiCollectionsAsync(
                ApiId, null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Upload

    protected MemoryStream SetupUploadOpenApiCollectionMutation(
        params IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        OpenApiClientMock.Setup(x => x.UploadOpenApiCollectionVersionAsync(
                OpenApiCollectionId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateUploadOpenApiCollectionPayload(errors));

        return capturedStream;
    }

    protected void SetupUploadOpenApiCollectionMutationException()
    {
        OpenApiClientMock.Setup(x => x.UploadOpenApiCollectionVersionAsync(
                OpenApiCollectionId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupUploadOpenApiCollectionMutationNullVersion()
    {
        var payload = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.OpenApiCollectionVersion)
            .Returns((IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_OpenApiCollectionVersion?)null);

        OpenApiClientMock.Setup(x => x.UploadOpenApiCollectionVersionAsync(
                OpenApiCollectionId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    #endregion

    #region Validate

    protected MemoryStream SetupValidateOpenApiCollectionMutation(
        params IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        OpenApiClientMock.Setup(x => x.StartOpenApiCollectionValidationAsync(
                OpenApiCollectionId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateValidateOpenApiCollectionPayload(errors));

        return capturedStream;
    }

    protected void SetupValidateOpenApiCollectionMutationException()
    {
        OpenApiClientMock.Setup(x => x.StartOpenApiCollectionValidationAsync(
                OpenApiCollectionId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupValidateOpenApiCollectionMutationNullRequestId()
    {
        var payload = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        OpenApiClientMock.Setup(x => x.StartOpenApiCollectionValidationAsync(
                OpenApiCollectionId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate>>? _openApiValidationSetup;

    protected void SetupValidateOpenApiCollectionSubscription(
        params IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateOpenApiCollectionValidationSuccessEvent()];
        }

        _openApiValidationSetup ??= OpenApiClientMock
            .SetupSequence(x => x.SubscribeToOpenApiCollectionValidationAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _openApiValidationSetup.Returns(events.ToAsyncEnumerable());
    }

    #endregion

    #region Publish

    protected void SetupPublishOpenApiCollectionMutation(
        bool force = false,
        bool waitForApproval = false,
        params IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors[] errors)
    {
        OpenApiClientMock.Setup(x => x.StartOpenApiCollectionPublishAsync(
                OpenApiCollectionId, Stage, Tag, force, waitForApproval, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreatePublishOpenApiCollectionPayload(errors));
    }

    protected void SetupPublishOpenApiCollectionMutationException()
    {
        OpenApiClientMock.Setup(x => x.StartOpenApiCollectionPublishAsync(
                OpenApiCollectionId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupPublishOpenApiCollectionMutationNullRequestId()
    {
        var payload = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        OpenApiClientMock.Setup(x => x.StartOpenApiCollectionPublishAsync(
                OpenApiCollectionId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate>>? _openApiPublishSetup;

    protected void SetupPublishOpenApiCollectionSubscription(
        params IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateOpenApiCollectionPublishSuccessEvent()];
        }

        _openApiPublishSetup ??= OpenApiClientMock
            .SetupSequence(x => x.SubscribeToOpenApiCollectionPublishAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _openApiPublishSetup.Returns(events.ToAsyncEnumerable());
    }

    #endregion

    #region Subscription Event Factories -- OpenApi Collection Publish

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishSuccessEvent()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OpenApiCollectionVersionPublishSuccess(
            "OpenApiCollectionVersionPublishSuccess",
            ProcessingState.Success);
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishFailedEvent(
            params IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors[] errors)
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OpenApiCollectionVersionPublishFailed(
            "OpenApiCollectionVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishOperationInProgressEvent()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishQueuedEvent(int queuePosition)
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishReadyEvent()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishApprovedEvent()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishWaitForApprovalEvent()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishFailedEventWithErrors()
    {
        var errorMock = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        MockErrorFactory.SetupUnexpectedProcessingError(errorMock, "Something went wrong during publish.");

        return CreateOpenApiCollectionPublishFailedEvent(errorMock.Object);
    }

    protected static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate
        CreateOpenApiCollectionPublishWaitForApprovalEventWithErrors()
    {
        var deploymentMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment>(
            MockBehavior.Strict);
        MockErrorFactory.SetupOpenApiCollectionDeployment(deploymentMock);

        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            deploymentMock.Object);
    }

    #endregion

    #region Subscription Event Factories -- OpenApi Collection Validation

    protected static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate
        CreateOpenApiCollectionValidationSuccessEvent()
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_OpenApiCollectionVersionValidationSuccess(
            "OpenApiCollectionVersionValidationSuccess",
            ProcessingState.Success);
    }

    protected static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate
        CreateOpenApiCollectionValidationFailedEvent(
            params IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors[] errors)
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_OpenApiCollectionVersionValidationFailed(
            "OpenApiCollectionVersionValidationFailed",
            ProcessingState.Failed,
            errors);
    }

    protected static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate
        CreateOpenApiCollectionValidationOperationInProgressEvent()
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    protected static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate
        CreateOpenApiCollectionValidationInProgressEvent()
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    protected static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate
        CreateOpenApiCollectionValidationFailedEventWithErrors()
    {
        var errorMock = new Mock<
            IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        MockErrorFactory.SetupOpenApiCollectionValidationError(errorMock);

        return CreateOpenApiCollectionValidationFailedEvent(errorMock.Object);
    }

    #endregion

    #region Payload Factories

    private static ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection CreateCreateOpenApiCollectionPayload(
        ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.OpenApiCollection)
                .Returns((ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_OpenApiCollection?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            payload.SetupGet(x => x.OpenApiCollection)
                .Returns(new CreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_OpenApiCollection_OpenApiCollection(
                    OpenApiCollectionName, OpenApiCollectionId));
            payload.SetupGet(x => x.Errors)
                .Returns(Array.Empty<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors>());
        }

        return payload.Object;
    }

    private static IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById CreateDeleteOpenApiCollectionPayload(
        IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors[] errors)
    {
        var payload = new Mock<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.OpenApiCollection)
                .Returns((IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_OpenApiCollection?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            payload.SetupGet(x => x.OpenApiCollection)
                .Returns(new DeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_OpenApiCollection_OpenApiCollection(
                    OpenApiCollectionName, OpenApiCollectionId));
            payload.SetupGet(x => x.Errors)
                .Returns(Array.Empty<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors>());
        }

        return payload.Object;
    }

    private static IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection CreateUploadOpenApiCollectionPayload(
        IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.Errors).Returns(errors);
            payload.SetupGet(x => x.OpenApiCollectionVersion)
                .Returns((IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_OpenApiCollectionVersion?)null);
        }
        else
        {
            var version = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_OpenApiCollectionVersion>(MockBehavior.Strict);
            version.SetupGet(x => x.Id).Returns("oav-1");

            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors>?)null);
            payload.SetupGet(x => x.OpenApiCollectionVersion).Returns(version.Object);
        }

        return payload.Object;
    }

    private static IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection CreateValidateOpenApiCollectionPayload(
        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection CreatePublishOpenApiCollectionPayload(
        IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    #endregion
}
