using ChilliCream.Nitro.Client;
using Moq;
using Moq.Language;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public abstract class ClientsCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string ClientId = "client-1";
    protected const string ClientName = "web-client";
    protected const string ApiName = "products";
    protected const string RequestId = "request-1";
    protected const string OperationsFile = "operations.json";

    protected void SetupOperationsFile()
    {
        SetupFile(OperationsFile, "{}");
    }

    #region ListClientsForPrompt

    protected void SetupListClientsForPrompt(
        params (string Id, string Name)[] clients)
    {
        var nodes = clients
            .Select(static c =>
            {
                var clientNode = new Mock<IListClientCommandQuery_Node_Clients_Edges_Node>(MockBehavior.Strict);
                clientNode.SetupGet(x => x.Id).Returns(c.Id);
                clientNode.SetupGet(x => x.Name).Returns(c.Name);
                return clientNode.Object;
            })
            .ToArray();

        ClientsClientMock.Setup(x => x.ListClientsAsync(
                ApiId, null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>(
                nodes, null, false));
    }

    #endregion

    #region Create

    protected void SetupCreateClientMutation(
        ICreateClientCommandMutation_CreateClient_Client? client = null,
        params ICreateClientCommandMutation_CreateClient_Errors[] errors)
    {
        ClientsClientMock.Setup(x => x.CreateClientAsync(
                ApiId, ClientName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreateClientPayload(client, errors));
    }

    protected void SetupCreateClientMutationException()
    {
        ClientsClientMock.Setup(x => x.CreateClientAsync(
                ApiId, ClientName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Delete

    protected void SetupDeleteClientMutation(
        IDeleteClientByIdCommandMutation_DeleteClientById_Client? client = null,
        params IDeleteClientByIdCommandMutation_DeleteClientById_Errors[] errors)
    {
        ClientsClientMock.Setup(x => x.DeleteClientAsync(
                ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteClientPayload(client, errors));
    }

    protected void SetupDeleteClientMutationException()
    {
        ClientsClientMock.Setup(x => x.DeleteClientAsync(
                ClientId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Show

    protected void SetupShowClientQuery(IShowClientCommandQuery_Node? result)
    {
        ClientsClientMock.Setup(x => x.GetClientAsync(
                ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    protected void SetupShowClientQueryException()
    {
        ClientsClientMock.Setup(x => x.GetClientAsync(
                ClientId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Upload

    protected MemoryStream SetupUploadClientMutation(
        params IUploadClient_UploadClient_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        ClientsClientMock.Setup(x => x.UploadClientVersionAsync(
                ClientId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateUploadClientPayload(errors));

        return capturedStream;
    }

    protected void SetupUploadClientMutationException()
    {
        ClientsClientMock.Setup(x => x.UploadClientVersionAsync(
                ClientId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupUploadClientMutationNullClientVersion()
    {
        var payload = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadClient_UploadClient_Errors>?)null);
        payload.SetupGet(x => x.ClientVersion)
            .Returns((IUploadClient_UploadClient_ClientVersion?)null);

        ClientsClientMock.Setup(x => x.UploadClientVersionAsync(
                ClientId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    #endregion

    #region Validate

    protected MemoryStream SetupValidateClientMutation(
        params IValidateClientVersion_ValidateClient_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        ClientsClientMock.Setup(x => x.StartClientValidationAsync(
                ClientId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateValidateClientPayload(errors));

        return capturedStream;
    }

    protected void SetupValidateClientMutationException()
    {
        ClientsClientMock.Setup(x => x.StartClientValidationAsync(
                ClientId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupValidateClientMutationNullRequestId()
    {
        var payload = new Mock<IValidateClientVersion_ValidateClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateClientVersion_ValidateClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        ClientsClientMock.Setup(x => x.StartClientValidationAsync(
                ClientId, Stage, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate>>? _clientValidationSetup;

    protected void SetupValidateClientSubscription(
        params IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateClientVersionValidationSuccessEvent()];
        }

        SetupClientVersionValidationUpdatedSubscription(events);
    }

    private void SetupClientVersionValidationUpdatedSubscription(
        IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[] events)
    {
        _clientValidationSetup ??= ClientsClientMock
            .SetupSequence(x => x.SubscribeToClientValidationAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _clientValidationSetup.Returns(events.ToAsyncEnumerable());
    }

    #endregion

    #region Publish

    protected void SetupPublishClientMutation(
        bool force = false,
        bool waitForApproval = false,
        params IPublishClientVersion_PublishClient_Errors[] errors)
    {
        ClientsClientMock.Setup(x => x.StartClientPublishAsync(
                ClientId, Stage, Tag, force, waitForApproval, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreatePublishClientPayload(errors));
    }

    protected void SetupPublishClientMutationException()
    {
        ClientsClientMock.Setup(x => x.StartClientPublishAsync(
                ClientId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupPublishClientMutationNullRequestId()
    {
        var payload = new Mock<IPublishClientVersion_PublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishClientVersion_PublishClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        ClientsClientMock.Setup(x => x.StartClientPublishAsync(
                ClientId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    private ISetupSequentialResult<IAsyncEnumerable<
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate>>? _clientPublishSetup;

    protected void SetupPublishClientSubscription(
        params IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateClientVersionPublishSuccessEvent()];
        }

        SetupClientVersionPublishUpdatedSubscription(events);
    }

    private void SetupClientVersionPublishUpdatedSubscription(
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[] events)
    {
        _clientPublishSetup ??= ClientsClientMock
            .SetupSequence(x => x.SubscribeToClientPublishAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _clientPublishSetup.Returns(events.ToAsyncEnumerable());
    }

    #endregion

    #region Unpublish

    protected void SetupUnpublishClientMutation(
        string tag = Tag,
        params IUnpublishClient_UnpublishClient_Errors[] errors)
    {
        ClientsClientMock.Setup(x => x.UnpublishClientVersionAsync(
                ClientId, Stage, tag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishClientPayload(errors));
    }

    protected void SetupUnpublishClientMutationException()
    {
        ClientsClientMock.Setup(x => x.UnpublishClientVersionAsync(
                ClientId, Stage, Tag, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupUnpublishClientMutationNullClientVersion()
    {
        var payload = new Mock<IUnpublishClient_UnpublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.ClientVersion)
            .Returns((IUnpublishClient_UnpublishClient_ClientVersion?)null);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUnpublishClient_UnpublishClient_Errors>?)null);

        ClientsClientMock.Setup(x => x.UnpublishClientVersionAsync(
                ClientId, Stage, Tag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    #endregion

    #region List

    protected void SetupListClientsQuery(
        string? cursor = null,
        int first = 10,
        params (string Id, string Name, string ApiName)[] clients)
    {
        var items = clients
            .Select(static c => CreateListClientNode(c.Id, c.Name, c.ApiName))
            .ToArray();

        ClientsClientMock.Setup(x => x.ListClientsAsync(
                ApiId, cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>(
                items, null, false));
    }

    protected void SetupListClientsQueryException()
    {
        ClientsClientMock.Setup(x => x.ListClientsAsync(
                ApiId, null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupListClientsQueryNotFound()
    {
        ClientsClientMock.Setup(x => x.ListClientsAsync(
                ApiId, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>?)null);
    }

    private static IListClientCommandQuery_Node_Clients_Edges_Node CreateListClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([apiName]);

        var clientNode = new Mock<IListClientCommandQuery_Node_Clients_Edges_Node>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
    }

    #endregion

    #region ListClientVersions

    protected void SetupListClientVersionsQuery(
        string? cursor = null,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Tag, DateTimeOffset CreatedAt, string[] Stages)[] versions)
    {
        var page = CreateListClientVersionsPage(endCursor, hasNextPage, versions);

        ClientsClientMock.Setup(x => x.ListClientVersionsAsync(
                ClientId, cursor, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
    }

    protected void SetupListClientVersionsQueryException()
    {
        ClientsClientMock.Setup(x => x.ListClientVersionsAsync(
                ClientId, null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupListClientVersionsQueryNotFound(string? cursor = null)
    {
        ClientsClientMock.Setup(x => x.ListClientVersionsAsync(
                ClientId, cursor, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionPage<IClientDetailPrompt_ClientVersionEdge>?)null);
    }

    protected static ConnectionPage<IClientDetailPrompt_ClientVersionEdge> CreateListClientVersionsPage(
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Tag, DateTimeOffset CreatedAt, string[] Stages)[] versions)
    {
        var items = versions
            .Select(static v => CreateVersionEdge(v.Tag, v.CreatedAt, v.Stages))
            .ToArray();

        return new ConnectionPage<IClientDetailPrompt_ClientVersionEdge>(items, endCursor, hasNextPage);
    }

    private static IClientDetailPrompt_ClientVersionEdge CreateVersionEdge(
        string tag,
        DateTimeOffset createdAt,
        string[] stages)
    {
        var publishedTo = stages
            .Select(static stageName =>
            {
                var stage = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node_PublishedTo_Stage>(MockBehavior.Strict);
                stage.SetupGet(x => x.Name).Returns(stageName);

                var published = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node_PublishedTo>(MockBehavior.Strict);
                published.SetupGet(x => x.Stage).Returns(stage.Object);

                return published.Object;
            })
            .ToArray();

        var node = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node>(MockBehavior.Strict);
        node.SetupGet(x => x.Tag).Returns(tag);
        node.SetupGet(x => x.CreatedAt).Returns(createdAt);
        node.SetupGet(x => x.PublishedTo).Returns(publishedTo);

        var edge = new Mock<IClientDetailPrompt_ClientVersionEdge>(MockBehavior.Strict);
        edge.SetupGet(x => x.Node).Returns(node.Object);

        return edge.Object;
    }

    #endregion

    #region Download

    protected void SetupDownloadPersistedQueries(Stream? result)
    {
        ClientsClientMock.Setup(x => x.DownloadPersistedQueriesAsync(
                ApiId, Stage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    protected void SetupDownloadPersistedQueriesException()
    {
        ClientsClientMock.Setup(x => x.DownloadPersistedQueriesAsync(
                ApiId, Stage, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Subscription Event Factories -- Client Version Publish

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishSuccessEvent()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishSuccess(
            "ClientVersionPublishSuccess",
            ProcessingState.Success);
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishFailedEvent(
            params IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors[] errors)
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishFailed(
            "ClientVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishOperationInProgressEvent()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishQueuedEvent(int queuePosition)
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishReadyEvent()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishApprovedEvent()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishWaitForApprovalEvent()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishFailedEventWithErrors()
    {
        return CreateClientVersionPublishFailedEvent(CreatePersistedQueryPublishError());
    }

    protected static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate
        CreateClientVersionPublishWaitForApprovalEventWithErrors()
    {
        var deploymentMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment>(
            MockBehavior.Strict);
        MockErrorFactory.SetupClientDeployment(deploymentMock);

        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            deploymentMock.Object);
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors
        CreatePersistedQueryPublishError()
    {
        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        MockErrorFactory.SetupClientPersistedQueryValidationError(errorMock);

        return errorMock.Object;
    }

    #endregion

    #region Subscription Event Factories -- Client Version Validation

    protected static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate
        CreateClientVersionValidationSuccessEvent()
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationSuccess(
            "ClientVersionValidationSuccess",
            ProcessingState.Success);
    }

    protected static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate
        CreateClientVersionValidationFailedEvent(
            params IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors[] errors)
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationFailed(
            "ClientVersionValidationFailed",
            ProcessingState.Failed,
            errors);
    }

    protected static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate
        CreateClientVersionValidationOperationInProgressEvent()
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    protected static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate
        CreateClientVersionValidationInProgressEvent()
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    protected static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate
        CreateClientVersionValidationFailedEventWithErrors()
    {
        var errorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        MockErrorFactory.SetupClientPersistedQueryValidationError(errorMock);

        return CreateClientVersionValidationFailedEvent(errorMock.Object);
    }

    #endregion

    #region Error Factories -- UploadClient

    protected static IUploadClient_UploadClient_Errors
        CreateUploadClientUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IUploadClient_UploadClient_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IUploadClient_UploadClient_Errors
        CreateUploadClientClientNotFoundError(string clientId = ClientId)
    {
        var mock = new Mock<IUploadClient_UploadClient_Errors_ClientNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Client '{clientId}' was not found.");
        mock.SetupGet(x => x.ClientId).Returns(clientId);
        return mock.Object;
    }

    protected static IUploadClient_UploadClient_Errors
        CreateUploadClientDuplicatedTagError(string tag = Tag)
    {
        var mock = new Mock<IUploadClient_UploadClient_Errors_DuplicatedTagError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Tag '{tag}' already exists.");
        return mock.Object;
    }

    protected static IUploadClient_UploadClient_Errors
        CreateUploadClientConcurrentOperationError()
    {
        var mock = new Mock<IUploadClient_UploadClient_Errors_ConcurrentOperationError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns("A concurrent operation is in progress.");
        return mock.Object;
    }

    #endregion

    #region Error Factories -- PublishClientVersion

    protected static IPublishClientVersion_PublishClient_Errors
        CreatePublishClientUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IPublishClientVersion_PublishClient_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IPublishClientVersion_PublishClient_Errors
        CreatePublishClientClientNotFoundError(string clientId = ClientId)
    {
        var mock = new Mock<IPublishClientVersion_PublishClient_Errors_ClientNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Client '{clientId}' was not found.");
        mock.SetupGet(x => x.ClientId).Returns(clientId);
        return mock.Object;
    }

    protected static IPublishClientVersion_PublishClient_Errors
        CreatePublishClientStageNotFoundError(string name = Stage)
    {
        var mock = new Mock<IPublishClientVersion_PublishClient_Errors_StageNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Stage '{name}' was not found.");
        mock.SetupGet(x => x.Name).Returns(name);
        return mock.Object;
    }

    protected static IPublishClientVersion_PublishClient_Errors
        CreatePublishClientVersionNotFoundError(string tag = Tag)
    {
        var mock = new Mock<IPublishClientVersion_PublishClient_Errors_ClientVersionNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns("Client version not found.");
        return mock.Object;
    }

    #endregion

    #region Error Factories -- ValidateClientVersion

    protected static IValidateClientVersion_ValidateClient_Errors
        CreateValidateClientUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IValidateClientVersion_ValidateClient_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IValidateClientVersion_ValidateClient_Errors
        CreateValidateClientClientNotFoundError(string clientId = ClientId)
    {
        var mock = new Mock<IValidateClientVersion_ValidateClient_Errors_ClientNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Client '{clientId}' was not found.");
        mock.SetupGet(x => x.ClientId).Returns(clientId);
        return mock.Object;
    }

    protected static IValidateClientVersion_ValidateClient_Errors
        CreateValidateClientStageNotFoundError(string name = Stage)
    {
        var mock = new Mock<IValidateClientVersion_ValidateClient_Errors_StageNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Stage '{name}' was not found.");
        mock.SetupGet(x => x.Name).Returns(name);
        return mock.Object;
    }

    #endregion

    #region Error Factories -- UnpublishClient

    protected static IUnpublishClient_UnpublishClient_Errors
        CreateUnpublishClientUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IUnpublishClient_UnpublishClient_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IUnpublishClient_UnpublishClient_Errors
        CreateUnpublishClientClientNotFoundError(string clientId = ClientId)
    {
        var mock = new Mock<IUnpublishClient_UnpublishClient_Errors_ClientNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Client '{clientId}' was not found.");
        mock.SetupGet(x => x.ClientId).Returns(clientId);
        return mock.Object;
    }

    protected static IUnpublishClient_UnpublishClient_Errors
        CreateUnpublishClientStageNotFoundError(string name = Stage)
    {
        var mock = new Mock<IUnpublishClient_UnpublishClient_Errors_StageNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Stage '{name}' was not found.");
        mock.SetupGet(x => x.Name).Returns(name);
        return mock.Object;
    }

    protected static IUnpublishClient_UnpublishClient_Errors
        CreateUnpublishClientConcurrentOperationError()
    {
        var mock = new Mock<IUnpublishClient_UnpublishClient_Errors_ConcurrentOperationError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns("A concurrent operation is in progress.");
        return mock.Object;
    }

    protected static IUnpublishClient_UnpublishClient_Errors
        CreateUnpublishClientVersionNotFoundError(string tag = Tag)
    {
        var mock = new Mock<IUnpublishClient_UnpublishClient_Errors_ClientVersionNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns("Client version not found.");
        return mock.Object;
    }

    #endregion

    #region Payload Factories

    private static ICreateClientCommandMutation_CreateClient CreateCreateClientPayload(
        ICreateClientCommandMutation_CreateClient_Client? client,
        ICreateClientCommandMutation_CreateClient_Errors[] errors)
    {
        var payload = new Mock<ICreateClientCommandMutation_CreateClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Client).Returns(client);
        payload.SetupGet(x => x.Errors).Returns(errors.Length > 0 ? errors : null);
        return payload.Object;
    }

    private static IDeleteClientByIdCommandMutation_DeleteClientById CreateDeleteClientPayload(
        IDeleteClientByIdCommandMutation_DeleteClientById_Client? client,
        IDeleteClientByIdCommandMutation_DeleteClientById_Errors[] errors)
    {
        var payload = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById>(MockBehavior.Strict);
        payload.SetupGet(x => x.Client).Returns(client);
        payload.SetupGet(x => x.Errors).Returns(errors.Length > 0 ? errors : null);
        return payload.Object;
    }

    private static IUploadClient_UploadClient CreateUploadClientPayload(
        IUploadClient_UploadClient_Errors[] errors)
    {
        var payload = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.Errors).Returns(errors);
            payload.SetupGet(x => x.ClientVersion)
                .Returns((IUploadClient_UploadClient_ClientVersion?)null);
        }
        else
        {
            var clientVersion = new Mock<IUploadClient_UploadClient_ClientVersion>(MockBehavior.Strict);
            clientVersion.SetupGet(x => x.Id).Returns("cv-1");

            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<IUploadClient_UploadClient_Errors>?)null);
            payload.SetupGet(x => x.ClientVersion).Returns(clientVersion.Object);
        }

        return payload.Object;
    }

    private static IValidateClientVersion_ValidateClient CreateValidateClientPayload(
        IValidateClientVersion_ValidateClient_Errors[] errors)
    {
        var payload = new Mock<IValidateClientVersion_ValidateClient>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static IPublishClientVersion_PublishClient CreatePublishClientPayload(
        IPublishClientVersion_PublishClient_Errors[] errors)
    {
        var payload = new Mock<IPublishClientVersion_PublishClient>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static IUnpublishClient_UnpublishClient CreateUnpublishClientPayload(
        IUnpublishClient_UnpublishClient_Errors[] errors)
    {
        var payload = new Mock<IUnpublishClient_UnpublishClient>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.ClientVersion)
                .Returns((IUnpublishClient_UnpublishClient_ClientVersion?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            var clientObj = new Mock<IUnpublishClient_UnpublishClient_ClientVersion_Client>(MockBehavior.Strict);
            clientObj.SetupGet(x => x.Name).Returns(ClientName);

            var clientVersion = new Mock<IUnpublishClient_UnpublishClient_ClientVersion>(MockBehavior.Strict);
            clientVersion.SetupGet(x => x.Id).Returns("cv-1");
            clientVersion.SetupGet(x => x.Client).Returns(clientObj.Object);

            payload.SetupGet(x => x.ClientVersion).Returns(clientVersion.Object);
            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<IUnpublishClient_UnpublishClient_Errors>?)null);
        }

        return payload.Object;
    }

    #endregion
}
