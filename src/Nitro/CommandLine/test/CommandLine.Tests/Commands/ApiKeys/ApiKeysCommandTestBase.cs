using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public abstract class ApiKeysCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string ApiKeyId = "key-1";
    protected const string ApiKeyName = "my-key";
    protected const string WorkspaceName = "Workspace";
    protected const string WorkspaceId = "ws-1";

    #region Create

    protected void SetupCreateApiKeyMutation(
        string name,
        string workspaceId,
        string? apiId = null,
        string? stageCondition = null,
        params ICreateApiKeyCommandMutation_CreateApiKey_Errors[] errors)
    {
        ApiKeysClientMock.Setup(x => x.CreateApiKeyAsync(
                name, workspaceId, apiId, stageCondition, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors.Length > 0
                ? CreateCreateApiKeyPayloadWithErrors(errors)
                : CreateCreateApiKeyPayload(name));
    }

    protected void SetupCreateApiKeyMutationException(
        string name,
        string workspaceId,
        string? apiId = null)
    {
        ApiKeysClientMock.Setup(x => x.CreateApiKeyAsync(
                name, workspaceId, apiId, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreateApiKeyMutationNullResult(
        string name,
        string workspaceId)
    {
        var payload = new Mock<ICreateApiKeyCommandMutation_CreateApiKey>();
        payload.SetupGet(x => x.Result)
            .Returns((ICreateApiKeyCommandMutation_CreateApiKey_Result?)null);
        payload.SetupGet(x => x.Errors).Returns([]);

        ApiKeysClientMock.Setup(x => x.CreateApiKeyAsync(
                name, workspaceId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
    }

    #endregion

    #region Delete

    protected void SetupDeleteApiKeyMutation(
        string keyId = ApiKeyId,
        params IDeleteApiKeyCommandMutation_DeleteApiKey_Errors[] errors)
    {
        ApiKeysClientMock.Setup(x => x.DeleteApiKeyAsync(
                keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors.Length > 0
                ? CreateDeleteApiKeyPayloadWithErrors(errors)
                : CreateDeleteApiKeyPayload(keyId));
    }

    protected void SetupDeleteApiKeyMutationException(string keyId = ApiKeyId)
    {
        ApiKeysClientMock.Setup(x => x.DeleteApiKeyAsync(
                keyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region List

    protected void SetupListApiKeysQuery(
        string workspaceId,
        string? cursor = null,
        int first = 10,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name, string WorkspaceName)[] apiKeys)
    {
        var items = apiKeys
            .Select(static key =>
                (IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node)
                new ListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node_ApiKey(
                    key.Id,
                    key.Name,
                    new ListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node_Workspace_Workspace(
                        key.WorkspaceName)))
            .ToArray();

        ApiKeysClientMock.Setup(x => x.ListApiKeysAsync(
                workspaceId, cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>(
                items, endCursor, hasNextPage));
    }

    protected void SetupListApiKeysQueryException(string workspaceId)
    {
        ApiKeysClientMock.Setup(x => x.ListApiKeysAsync(
                workspaceId, null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Payload Factories

    private static ICreateApiKeyCommandMutation_CreateApiKey CreateCreateApiKeyPayload(
        string name)
    {
        var workspace = new Mock<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node_Workspace_Workspace>();
        workspace.SetupGet(x => x.Name).Returns(WorkspaceName);

        var key = new Mock<ICreateApiKeyCommandMutation_CreateApiKey_Result_Key_ApiKey>();
        key.SetupGet(x => x.Id).Returns(ApiKeyId);
        key.SetupGet(x => x.Name).Returns(name);
        key.SetupGet(x => x.Workspace).Returns(workspace.Object);

        var result = new Mock<ICreateApiKeyCommandMutation_CreateApiKey_Result_ApiKeyWithSecret>();
        result.SetupGet(x => x.Secret).Returns("secret-123");
        result.SetupGet(x => x.Key).Returns(key.Object);

        var payload = new Mock<ICreateApiKeyCommandMutation_CreateApiKey>();
        payload.SetupGet(x => x.Result).Returns(result.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }

    private static ICreateApiKeyCommandMutation_CreateApiKey CreateCreateApiKeyPayloadWithErrors(
        ICreateApiKeyCommandMutation_CreateApiKey_Errors[] errors)
    {
        var payload = new Mock<ICreateApiKeyCommandMutation_CreateApiKey>();
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    private static IDeleteApiKeyCommandMutation_DeleteApiKey CreateDeleteApiKeyPayload(
        string keyId)
    {
        var workspace = new Mock<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node_Workspace_Workspace>();
        workspace.SetupGet(x => x.Name).Returns(WorkspaceName);

        var key = new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey_ApiKey_ApiKey>();
        key.SetupGet(x => x.Id).Returns(keyId);
        key.SetupGet(x => x.Name).Returns(ApiKeyName);
        key.SetupGet(x => x.Workspace).Returns(workspace.Object);

        var payload = new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey>();
        payload.SetupGet(x => x.ApiKey).Returns(key.Object);
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<IDeleteApiKeyCommandMutation_DeleteApiKey_Errors>());
        return payload.Object;
    }

    private static IDeleteApiKeyCommandMutation_DeleteApiKey CreateDeleteApiKeyPayloadWithErrors(
        IDeleteApiKeyCommandMutation_DeleteApiKey_Errors[] errors)
    {
        var payload = new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey>();
        payload.SetupGet(x => x.ApiKey).Returns((IDeleteApiKeyCommandMutation_DeleteApiKey_ApiKey?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    #endregion
}
