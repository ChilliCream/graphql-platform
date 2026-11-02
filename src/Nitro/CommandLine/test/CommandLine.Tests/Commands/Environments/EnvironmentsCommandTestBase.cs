using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public abstract class EnvironmentsCommandTestBase(NitroCommandFixture fixture)
    : CommandTestBase(fixture)
{
    protected const string EnvironmentId = "env-1";
    protected const string EnvironmentName = "production";
    protected const string WorkspaceId = "ws-1";
    protected const string WorkspaceName = "workspace-a";

    #region Show

    protected void SetupGetEnvironmentQuery(
        string environmentId,
        IShowEnvironmentCommandQuery_Node? result)
    {
        EnvironmentsClientMock.Setup(x => x.GetEnvironmentAsync(
                environmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    protected void SetupGetEnvironmentQueryException(string environmentId)
    {
        EnvironmentsClientMock.Setup(x => x.GetEnvironmentAsync(
                environmentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region List

    protected void SetupListEnvironmentsQuery(
        string workspaceId,
        string? cursor = null,
        int first = 10,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name, string WorkspaceName)[] environments)
    {
        var items = environments
            .Select(static env => CreateEnvironmentNode(env.Id, env.Name, env.WorkspaceName))
            .ToArray();

        EnvironmentsClientMock.Setup(x => x.ListEnvironmentsAsync(
                workspaceId, cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>(
                items, endCursor, hasNextPage));
    }

    protected void SetupListEnvironmentsQueryException(string workspaceId)
    {
        EnvironmentsClientMock.Setup(x => x.ListEnvironmentsAsync(
                workspaceId, null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Create

    protected void SetupCreateEnvironmentMutation(
        string workspaceId,
        string name)
    {
        EnvironmentsClientMock.Setup(x => x.CreateEnvironmentAsync(
                workspaceId, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessPayload());
    }

    protected void SetupCreateEnvironmentMutationException(
        string workspaceId,
        string name)
    {
        EnvironmentsClientMock.Setup(x => x.CreateEnvironmentAsync(
                workspaceId, name, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreateEnvironmentMutationNoChanges(
        string workspaceId,
        string name)
    {
        EnvironmentsClientMock.Setup(x => x.CreateEnvironmentAsync(
                workspaceId, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(changes: [], errors: null));
    }

    protected void SetupCreateEnvironmentMutationWithChangeError(
        string workspaceId,
        string name,
        ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Error error)
    {
        EnvironmentsClientMock.Setup(x => x.CreateEnvironmentAsync(
                workspaceId, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(
                changes: [CreateChange(result: null, error: error)],
                errors: null));
    }

    protected void SetupCreateEnvironmentMutationWithErrors(
        string workspaceId,
        string name,
        params ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Errors[] errors)
    {
        EnvironmentsClientMock.Setup(x => x.CreateEnvironmentAsync(
                workspaceId, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(changes: null, errors: errors));
    }

    protected void SetupCreateEnvironmentMutationWithWrongResultType(
        string workspaceId,
        string name)
    {
        var wrongResult = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result_ApiDocument>(
            MockBehavior.Strict);

        EnvironmentsClientMock.Setup(x => x.CreateEnvironmentAsync(
                workspaceId, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(
                changes: [CreateChange(wrongResult.Object, error: null)],
                errors: null));
    }

    #endregion

    #region Node Factories

    protected static IShowEnvironmentCommandQuery_Node CreateShowEnvironmentNode(
        string id,
        string name,
        string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace>(
            MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var node = new Mock<IShowEnvironmentCommandQuery_Node_Environment>(MockBehavior.Strict);
        node.SetupGet(x => x.Id).Returns(id);
        node.SetupGet(x => x.Name).Returns(name);
        node.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return node.Object;
    }

    private static IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node
        CreateEnvironmentNode(string id, string name, string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace>(
            MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var node = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>(MockBehavior.Strict);
        node.SetupGet(x => x.Id).Returns(id);
        node.SetupGet(x => x.Name).Returns(name);
        node.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return node.Object;
    }

    #endregion

    #region Payload Factories

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges CreateSuccessPayload()
    {
        return CreatePayload(
            changes: [CreateChange(CreateEnvironmentResult(), error: null)],
            errors: null);
    }

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges CreatePayload(
        IReadOnlyList<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes>? changes,
        IReadOnlyList<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Errors>? errors)
    {
        var payload = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns(changes);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes CreateChange(
        ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result? result,
        ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Error? error)
    {
        var change = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes>(MockBehavior.Strict);
        change.SetupGet(x => x.ReferenceId).Returns("env");
        change.SetupGet(x => x.Result).Returns(result);
        change.SetupGet(x => x.Error).Returns(error);
        return change.Object;
    }

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result_Environment
        CreateEnvironmentResult()
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace>(
            MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns(WorkspaceName);

        var result = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result_Environment>(
            MockBehavior.Strict);
        result.SetupGet(x => x.Id).Returns(EnvironmentId);
        result.SetupGet(x => x.Name).Returns(EnvironmentName);
        result.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return result.Object;
    }

    #endregion
}
