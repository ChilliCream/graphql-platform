using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class SetDefaultWorkspaceCommandTests
{
    [Fact]
    public async Task SetDefault_NoWorkspaces_ReturnsError()
    {
        // arrange
        var page = new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
            [],
            EndCursor: null,
            HasNextPage: false);
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client, new TestSessionService());

        // act
        var exitCode = await host.InvokeAsync("workspace", "set-default");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You do not have any workspaces. Run `[bold blue]nitro launch[/]` and create one.
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_SingleWorkspaceWithoutForce_SetsDefaultWithoutPrompt()
    {
        // arrange
        var page = new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
            [CreateWorkspaceSelection("ws-1", "Workspace")],
            EndCursor: null,
            HasNextPage: false);

        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var session = new TestSessionService();
        var host = new CommandBuilder();
        var console = new NitroConsole(host.Console);

        // act
        var exitCode = await SetDefaultWorkspaceCommand.ExecuteAsync(
            forceSelection: false,
            console,
            client.Object,
            session,
            CancellationToken.None);

        // assert
        Assert.Equal(0, exitCode);
        Assert.NotNull(session.Session?.Workspace);
        Assert.Equal("ws-1", session.Session?.Workspace?.Id);
        Assert.Equal("Workspace", session.Session?.Workspace?.Name);
        Assert.Equal(string.Empty, host.Output.Trim());
        client.VerifyAll();
    }

    private static CommandBuilder CreateHost(
        Mock<IWorkspacesClient> client,
        TestSessionService session)
    {
        var host = new CommandBuilder()
            .AddService(client.Object)
            .AddService<ISessionService>(session);

        return host;
    }

    private static ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace
        CreateWorkspaceSelection(
            string id,
            string name)
    {
        var workspace = new Mock<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace>();
        workspace.SetupGet(x => x.Id).Returns(id);
        workspace.SetupGet(x => x.Name).Returns(name);

        return workspace.Object;
    }
}
