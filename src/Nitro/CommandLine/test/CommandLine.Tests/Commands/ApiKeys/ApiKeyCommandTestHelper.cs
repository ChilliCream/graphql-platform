using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

internal static class ApiKeyCommandTestHelper
{
    public static ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node> CreateListApiKeysPage(
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

        return new ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>(
            items,
            endCursor,
            hasNextPage);
    }

    public static ICreateApiKeyCommandMutation_CreateApiKey CreateApiKeyResult(
        string secret,
        string keyId,
        string keyName,
        string workspaceName)
    {
        var workspace = new Mock<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node_Workspace_Workspace>();
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var key = new Mock<ICreateApiKeyCommandMutation_CreateApiKey_Result_Key_ApiKey>();
        key.SetupGet(x => x.Id).Returns(keyId);
        key.SetupGet(x => x.Name).Returns(keyName);
        key.SetupGet(x => x.Workspace).Returns(workspace.Object);

        var result = new Mock<ICreateApiKeyCommandMutation_CreateApiKey_Result_ApiKeyWithSecret>();
        result.SetupGet(x => x.Secret).Returns(secret);
        result.SetupGet(x => x.Key).Returns(key.Object);

        var payload = new Mock<ICreateApiKeyCommandMutation_CreateApiKey>();
        payload.SetupGet(x => x.Result).Returns(result.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }

    public static ICreateApiKeyCommandMutation_CreateApiKey CreateApiKeyResultWithErrors(
        params ICreateApiKeyCommandMutation_CreateApiKey_Errors[] errors)
    {
        var payload = new Mock<ICreateApiKeyCommandMutation_CreateApiKey>();
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    public static IDeleteApiKeyCommandMutation_DeleteApiKey CreateDeleteApiKeyResult(
        string keyId,
        string keyName,
        string workspaceName)
    {
        var workspace = new Mock<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node_Workspace_Workspace>();
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var key = new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey_ApiKey_ApiKey>();
        key.SetupGet(x => x.Id).Returns(keyId);
        key.SetupGet(x => x.Name).Returns(keyName);
        key.SetupGet(x => x.Workspace).Returns(workspace.Object);

        var payload = new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey>();
        payload.SetupGet(x => x.ApiKey).Returns(key.Object);
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<IDeleteApiKeyCommandMutation_DeleteApiKey_Errors>());
        return payload.Object;
    }

    public static IDeleteApiKeyCommandMutation_DeleteApiKey CreateDeleteApiKeyResultWithErrors(
        params IDeleteApiKeyCommandMutation_DeleteApiKey_Errors[] errors)
    {
        var payload = new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey>();
        payload.SetupGet(x => x.ApiKey).Returns((IDeleteApiKeyCommandMutation_DeleteApiKey_ApiKey?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }
}
