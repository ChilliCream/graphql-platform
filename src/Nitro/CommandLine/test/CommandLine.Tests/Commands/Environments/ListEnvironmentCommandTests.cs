using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class ListEnvironmentCommandTests
{
    [Fact]
    public async Task List_MissingWorkspace_ReturnsError()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        var host = CreateHost(client, NoSession());

        // act
        var exitCode = await host.InvokeAsync(
            "environment",
            "list",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or specify the workspace ID with the --workspace-id option (if available).
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var page = new ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>(
            [CreateEnvironmentNode("env-1", "prod", "Workspace")],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "environment",
            "list",
            "--workspace-id",
            "ws-1",
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
                  "id": "env-1",
                  "name": "prod",
                  "workspace": {
                    "name": "Workspace"
                  }
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
        var page = new ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>(
            [],
            EndCursor: null,
            HasNextPage: false);
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client, session: null);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "environment",
            "list",
            "--workspace-id",
            "ws-1");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandBuilder CreateHost(
        Mock<IEnvironmentsClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IEnvironmentsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static TestSessionService NoSession() => new();

    private static IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Environment
        CreateEnvironmentNode(
            string id,
            string name,
            string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace_Workspace>();
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var environment = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Environment>();
        environment.SetupGet(x => x.Id).Returns(id);
        environment.SetupGet(x => x.Name).Returns(name);
        environment.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return environment.Object;
    }
}
