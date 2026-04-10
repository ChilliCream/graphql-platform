using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public abstract class WorkspacesCommandTestBase(NitroCommandFixture fixture)
    : CommandTestBase(fixture)
{
    protected const string WorkspaceId = "ws-1";
    protected const string WorkspaceName = "my-workspace";

    #region Show / GetWorkspace

    protected void SetupGetWorkspaceQuery(
        string workspaceId,
        IShowWorkspaceCommandQuery_Node? result)
    {
        WorkspacesClientMock.Setup(x => x.GetWorkspaceAsync(
                workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    protected void SetupGetWorkspaceQueryException(string workspaceId)
    {
        WorkspacesClientMock.Setup(x => x.GetWorkspaceAsync(
                workspaceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region List

    protected void SetupListWorkspacesQuery(
        string? cursor = null,
        int first = 10,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name, bool Personal)[] workspaces)
    {
        var items = workspaces
            .Select(static ws =>
                (IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node)
                new ListWorkspaceCommandQuery_Me_Workspaces_Edges_Node_Workspace(
                    ws.Id, ws.Name, ws.Personal))
            .ToArray();

        WorkspacesClientMock.Setup(x => x.ListWorkspacesAsync(
                cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>(
                items, endCursor, hasNextPage));
    }

    protected void SetupListWorkspacesQueryException()
    {
        WorkspacesClientMock.Setup(x => x.ListWorkspacesAsync(
                null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Create

    protected void SetupCreateWorkspaceMutation(
        params ICreateWorkspaceCommandMutation_CreateWorkspace_Errors[] errors)
    {
        WorkspacesClientMock.Setup(x => x.CreateWorkspaceAsync(
                WorkspaceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors.Length > 0
                ? CreateCreateWorkspacePayloadWithErrors(errors)
                : CreateCreateWorkspacePayload());
    }

    protected void SetupCreateWorkspaceMutationException()
    {
        WorkspacesClientMock.Setup(x => x.CreateWorkspaceAsync(
                WorkspaceName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreateWorkspaceMutationNullWorkspace()
    {
        WorkspacesClientMock.Setup(x => x.CreateWorkspaceAsync(
                WorkspaceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreateWorkspacePayloadWithNullResult());
    }

    #endregion

    #region SelectWorkspaces

    protected void SetupSelectWorkspacesQuery(
        params ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node[] nodes)
    {
        WorkspacesClientMock.Setup(x => x.SelectWorkspacesAsync(
                null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
                nodes, null, false));
    }

    protected void SetupSelectWorkspacesQueryException()
    {
        WorkspacesClientMock.Setup(x => x.SelectWorkspacesAsync(
                null, 5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Node Factories

    protected static IShowWorkspaceCommandQuery_Node CreateShowWorkspaceNode(
        string id,
        string name,
        bool personal)
        => new ShowWorkspaceCommandQuery_Node_Workspace(id, name, personal);

    #endregion

    #region Error Factories -- CreateWorkspace

    protected static ICreateWorkspaceCommandMutation_CreateWorkspace_Errors
        CreateCreateWorkspaceUnauthorizedError()
    {
        return new CreateWorkspaceCommandMutation_CreateWorkspace_Errors_UnauthorizedOperation(
            "UnauthorizedOperation", "Not authorized");
    }

    protected static ICreateWorkspaceCommandMutation_CreateWorkspace_Errors
        CreateCreateWorkspaceValidationError()
    {
        return new CreateWorkspaceCommandMutation_CreateWorkspace_Errors_ValidationError(
            "ValidationError", "Name is required", []);
    }

    #endregion

    #region Payload Factories

    private static ICreateWorkspaceCommandMutation_CreateWorkspace CreateCreateWorkspacePayload()
    {
        var payload = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace>(MockBehavior.Strict);
        payload.SetupGet(x => x.Workspace)
            .Returns(new CreateWorkspaceCommandMutation_CreateWorkspace_Workspace_Workspace(
                WorkspaceId, WorkspaceName, false));
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateWorkspaceCommandMutation_CreateWorkspace_Errors>());
        return payload.Object;
    }

    private static ICreateWorkspaceCommandMutation_CreateWorkspace CreateCreateWorkspacePayloadWithNullResult()
    {
        var payload = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace>(MockBehavior.Strict);
        payload.SetupGet(x => x.Workspace)
            .Returns((ICreateWorkspaceCommandMutation_CreateWorkspace_Workspace?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateWorkspaceCommandMutation_CreateWorkspace_Errors>());
        return payload.Object;
    }

    private static ICreateWorkspaceCommandMutation_CreateWorkspace CreateCreateWorkspacePayloadWithErrors(
        params ICreateWorkspaceCommandMutation_CreateWorkspace_Errors[] errors)
    {
        var payload = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace>(MockBehavior.Strict);
        payload.SetupGet(x => x.Workspace)
            .Returns((ICreateWorkspaceCommandMutation_CreateWorkspace_Workspace?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    #endregion
}
