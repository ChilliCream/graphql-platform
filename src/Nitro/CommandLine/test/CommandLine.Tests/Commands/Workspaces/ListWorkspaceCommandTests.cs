using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class ListWorkspaceCommandTests
{
    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var page = new ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>(
            [CreateWorkspaceNode("ws-1", "Workspace", personal: false)],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "workspace",
            "list",
            "--cursor",
            "cursor-start",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "id": "ws-1",
                  "name": "Workspace",
                  "personal": false
                }
              ],
              "cursor": "cursor-1"
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task List_InteractivePath_UsesPagedTableBranch()
    {
        // arrange
        var page = new ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>(
            [],
            EndCursor: null,
            HasNextPage: false);
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync("workspace", "list");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(Mock<IWorkspacesClient> client)
    {
        var host = new CommandTestHost()
            .AddService(client.Object)
            .AddService<ISessionService>(TestSessionService.WithWorkspace());

        return host;
    }

    private static IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node_Workspace CreateWorkspaceNode(
        string id,
        string name,
        bool personal)
    {
        var workspace = new Mock<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node_Workspace>();
        workspace.SetupGet(x => x.Id).Returns(id);
        workspace.SetupGet(x => x.Name).Returns(name);
        workspace.SetupGet(x => x.Personal).Returns(personal);

        return workspace.Object;
    }
}
