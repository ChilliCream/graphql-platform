using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;
using UserSession = ChilliCream.Nitro.CommandLine.Services.Sessions.Session;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Session;

public abstract class SessionCommandTestBase : CommandTestBase
{
    protected SessionCommandTestBase(NitroCommandFixture fixture) : base(fixture)
    {
        SetupNoAuthentication();
    }

    #region Login

    protected void SetupLogin()
    {
        _sessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLoginSession());
    }

    protected void SetupLogin(string url)
    {
        _sessionServiceMock
            .Setup(x => x.LoginAsync(
                url,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLoginSession());
    }

    protected void SetupLoginReturnsNull()
    {
        _sessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<UserSession>(null!));
    }

    protected void SetupLoginThrows(string message)
    {
        _sessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExitException(message));
    }

    #endregion

    #region Logout

    protected void SetupLogout()
    {
        _sessionServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected void SetupLogoutThrows()
    {
        _sessionServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Workspace Selection

    protected void SetupSelectWorkspaces(
        params ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node[] workspaces)
    {
        WorkspacesClientMock.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
                workspaces, null, false));
    }

    protected void SetupSelectWorkspace(string workspaceId, string workspaceName)
    {
        _sessionServiceMock
            .Setup(x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == workspaceId && w.Name == workspaceName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLoginSession(new Workspace(workspaceId, workspaceName)));
    }

    protected void SetupSelectWorkspaceAny()
    {
        _sessionServiceMock
            .Setup(x => x.SelectWorkspaceAsync(
                It.IsAny<Workspace>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLoginSession(new Workspace("ws-1", "my-workspace")));
    }

    #endregion

    #region Session

    protected void SetupCustomSession(
        string apiUrl = "api.chillicream.com",
        string identityUrl = "https://id.chillicream.com",
        string? workspaceId = null,
        string? workspaceName = null)
    {
        var workspace = workspaceId is not null && workspaceName is not null
            ? new Workspace(workspaceId, workspaceName)
            : null;

        _sessionServiceMock
            .SetupGet(x => x.Session)
            .Returns(new UserSession(
                "session-1",
                "subject-1",
                "tenant-1",
                identityUrl,
                apiUrl,
                "user@chillicream.com",
                tokens: null,
                workspace: workspace));
    }

    #endregion

    #region Factories

    protected static ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node
        CreateWorkspaceNode(string id, string name)
    {
        return new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
            id, name, false);
    }

    private static UserSession CreateLoginSession(Workspace? workspace = null)
    {
        return new UserSession(
            "session-1",
            "subject-1",
            "tenant-1",
            "https://id.chillicream.com",
            "api.chillicream.com",
            "user@test.com",
            new Tokens(
                "access-token",
                "id-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddHours(1)),
            workspace);
    }

    #endregion
}
