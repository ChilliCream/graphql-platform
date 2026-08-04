using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public abstract class ApisCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string ApiName = "my-api";
    protected const string WorkspaceName = "Workspace";
    protected const string WorkspaceId = "ws-1";

    #region Show

    protected void SetupShowApiQuery(IShowApiCommandQuery_Node? result)
    {
        ApisClientMock.Setup(x => x.GetApiAsync(
                ApiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    protected void SetupShowApiQueryException()
    {
        ApisClientMock.Setup(x => x.GetApiAsync(
                ApiId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Create

    protected void SetupCreateApiMutation(
        string workspaceId,
        string name,
        IReadOnlyList<string>? expectedPath = null,
        ApiKind? kind = null)
    {
        if (expectedPath is not null)
        {
            ApisClientMock.Setup(x => x.CreateApiAsync(
                    workspaceId,
                    It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(expectedPath)),
                    name,
                    kind,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateApiSuccessPayload());
        }
        else
        {
            ApisClientMock.Setup(x => x.CreateApiAsync(
                    workspaceId,
                    It.IsAny<IReadOnlyList<string>>(),
                    name,
                    kind,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateApiSuccessPayload());
        }
    }

    protected void SetupCreateApiMutationException(
        string workspaceId,
        string name)
    {
        ApisClientMock.Setup(x => x.CreateApiAsync(
                workspaceId,
                It.IsAny<IReadOnlyList<string>>(),
                name,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreateApiMutationNoChanges(
        string workspaceId,
        string name)
    {
        ApisClientMock.Setup(x => x.CreateApiAsync(
                workspaceId,
                It.IsAny<IReadOnlyList<string>>(),
                name,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithNoChanges());
    }

    protected void SetupCreateApiMutationWithChangeError(
        string workspaceId,
        string name,
        ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error error)
    {
        ApisClientMock.Setup(x => x.CreateApiAsync(
                workspaceId,
                It.IsAny<IReadOnlyList<string>>(),
                name,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithChangeError(error));
    }

    protected void SetupCreateApiMutationWithErrors(
        string workspaceId,
        string name,
        params ICreateApiCommandMutation_PushWorkspaceChanges_Errors[] errors)
    {
        ApisClientMock.Setup(x => x.CreateApiAsync(
                workspaceId,
                It.IsAny<IReadOnlyList<string>>(),
                name,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithMutationErrors(errors));
    }

    #endregion

    #region Delete

    protected void SetupGetApiForDeleteQuery(string apiId, string name)
    {
        ApisClientMock.Setup(x => x.GetApiForDeleteAsync(
                apiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteApiCommandQuery_Node_Api(name, "v1", workspace: null));
    }

    protected void SetupGetApiForDeleteQueryNull(string apiId)
    {
        ApisClientMock.Setup(x => x.GetApiForDeleteAsync(
                apiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDeleteApiCommandQuery_Node?)null);
    }

    protected void SetupDeleteApiMutation(
        string apiId,
        string name,
        IReadOnlyList<string> path,
        params IDeleteApiCommandMutation_DeleteApiById_Errors[] errors)
    {
        ApisClientMock.Setup(x => x.DeleteApiAsync(
                apiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors.Length > 0
                ? new DeleteApiCommandMutation_DeleteApiById_DeleteApiByIdPayload(api: null, errors)
                : new DeleteApiCommandMutation_DeleteApiById_DeleteApiByIdPayload(
                    new DeleteApiCommandMutation_DeleteApiById_Api_Api(
                        name, apiId, path,
                        new ShowApiCommandQuery_Node_Workspace_Workspace(WorkspaceId, WorkspaceName),
                        CreateSettings()),
                    errors: []));
    }

    protected void SetupDeleteApiMutationNullApi(string apiId)
    {
        ApisClientMock.Setup(x => x.DeleteApiAsync(
                apiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteApiCommandMutation_DeleteApiById_DeleteApiByIdPayload(
                api: null, errors: []));
    }

    protected void SetupDeleteApiMutationException(string apiId)
    {
        ApisClientMock.Setup(x => x.DeleteApiAsync(
                apiId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region List

    protected void SetupListApisQuery(
        string workspaceId,
        string? cursor = null,
        int first = 10,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name, IReadOnlyList<string> Path, string WorkspaceName)[] apis)
    {
        var items = apis
            .Select(static api =>
                (IListApiCommandQuery_WorkspaceById_Apis_Edges_Node)
                new ListApiCommandQuery_WorkspaceById_Apis_Edges_Node_Api(
                    api.Id,
                    api.Name,
                    api.Path,
                    new ShowApiCommandQuery_Node_Workspace_Workspace(WorkspaceId, api.WorkspaceName),
                    CreateSettings()))
            .ToArray();

        ApisClientMock.Setup(x => x.ListApisAsync(
                workspaceId, cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>(
                items, endCursor, hasNextPage));
    }

    protected void SetupListApisQueryException(string workspaceId)
    {
        ApisClientMock.Setup(x => x.ListApisAsync(
                workspaceId, null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region SetSettings

    protected void SetupUpdateApiSettingsMutation(
        string apiId,
        bool treatDangerousAsBreaking,
        bool allowBreakingSchemaChanges,
        string name = ApiName,
        params ISetApiSettingsCommandMutation_UpdateApiSettings_Errors[] errors)
    {
        ApisClientMock.Setup(x => x.UpdateApiSettingsAsync(
                apiId, treatDangerousAsBreaking, allowBreakingSchemaChanges, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors.Length > 0
                ? new SetApiSettingsCommandMutation_UpdateApiSettings_UpdateApiSettingsPayload(api: null, errors)
                : new SetApiSettingsCommandMutation_UpdateApiSettings_UpdateApiSettingsPayload(
                    new SetApiSettingsCommandMutation_UpdateApiSettings_Api_Api(
                        name, ["products"], apiId,
                        new ShowApiCommandQuery_Node_Workspace_Workspace(WorkspaceId, WorkspaceName),
                        CreateSettings(treatDangerousAsBreaking, allowBreakingSchemaChanges)),
                    errors: []));
    }

    protected void SetupUpdateApiSettingsMutationException(
        string apiId,
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
    {
        ApisClientMock.Setup(x => x.UpdateApiSettingsAsync(
                apiId, treatDangerousAsBreaking, allowBreakingSchemaChanges, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupUpdateApiSettingsMutationNullResult(
        string apiId,
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
    {
        ApisClientMock.Setup(x => x.UpdateApiSettingsAsync(
                apiId, treatDangerousAsBreaking, allowBreakingSchemaChanges, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SetApiSettingsCommandMutation_UpdateApiSettings_UpdateApiSettingsPayload(
                api: null, errors: []));
    }

    #endregion

    #region Node Factories

    protected static IShowApiCommandQuery_Node CreateShowApiNode(
        string id,
        string name,
        IReadOnlyList<string> path,
        string workspaceName = WorkspaceName,
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
        => new ShowApiCommandQuery_Node_Api(
            id, name, path,
            new ShowApiCommandQuery_Node_Workspace_Workspace(WorkspaceId, workspaceName),
            CreateSettings(treatDangerousAsBreaking, allowBreakingSchemaChanges));

    #endregion

    #region Payload Factories

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiSuccessPayload()
    {
        var change = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>(MockBehavior.Strict);
        change.SetupGet(x => x.Error).Returns((ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error?)null);

        var workspace = new Mock<IShowApiCommandQuery_Node_Workspace_1>(MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns(WorkspaceName);

        var result = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Result_Api>(MockBehavior.Strict);
        result.SetupGet(x => x.Id).Returns(ApiId);
        result.SetupGet(x => x.Name).Returns(ApiName);
        result.SetupGet(x => x.Path).Returns(["products", "catalog"]);
        result.SetupGet(x => x.Workspace).Returns(workspace.Object);
        result.SetupGet(x => x.Settings).Returns(CreateSettings());

        change.SetupGet(x => x.Result).Returns(result.Object);

        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns([change.Object]);
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>());

        return payload.Object;
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiPayloadWithNoChanges()
    {
        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>());
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>());
        return payload.Object;
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiPayloadWithChangeError(
        ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error error)
    {
        var change = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>(MockBehavior.Strict);
        change.SetupGet(x => x.Error).Returns(error);
        change.SetupGet(x => x.Result).Returns((ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Result?)null);

        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns([change.Object]);
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>());
        return payload.Object;
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiPayloadWithMutationErrors(
        params ICreateApiCommandMutation_PushWorkspaceChanges_Errors[] errors)
    {
        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>());
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    private static IShowApiCommandQuery_Node_Settings CreateSettings(
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
        => new ShowApiCommandQuery_Node_Settings_ApiSettings(
            new ShowApiCommandQuery_Node_Settings_SchemaRegistry_SchemaRegistrySettings(
                treatDangerousAsBreaking,
                allowBreakingSchemaChanges));

    #endregion

    #region Error Factories -- UpdateApiSettings

    protected static ISetApiSettingsCommandMutation_UpdateApiSettings_Errors
        CreateUpdateApiSettingsApiNotFoundError()
    {
        return new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_ApiNotFoundError(
            "ApiNotFoundError",
            "API not found",
            ApiId);
    }

    protected static ISetApiSettingsCommandMutation_UpdateApiSettings_Errors
        CreateUpdateApiSettingsUnauthorizedError()
    {
        return new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_UnauthorizedOperation(
            "UnauthorizedOperation",
            "Not authorized");
    }

    protected static ISetApiSettingsCommandMutation_UpdateApiSettings_Errors
        CreateUpdateApiSettingsUnknownError(string message = "payload denied")
    {
        var error = new Mock<ISetApiSettingsCommandMutation_UpdateApiSettings_Errors>(MockBehavior.Strict);
        error.As<IError>()
            .SetupGet(x => x.Message)
            .Returns(message);
        return error.Object;
    }

    #endregion
}
