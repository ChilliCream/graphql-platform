using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class ListApiCommandTests
{
    [Fact]
    public async Task List_MissingWorkspace_ReturnsError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        var host = ApiCommandTestHelper.CreateHost(client, session: new TestSessionService());

        // act
        var exitCode = await host.InvokeAsync(
            "api",
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
        var page = new ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>(
            [ApiCommandTestHelper.CreateListApiNode("api-1", "products", ["catalog"])],
            EndCursor: "cursor-1",
            HasNextPage: false);

        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "ws-1",
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = ApiCommandTestHelper.CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
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
                  "id": "api-1",
                  "name": "products",
                  "path": "catalog",
                  "workspace": {
                    "name": "Workspace"
                  },
                  "apiDetailPromptSettings": {
                    "apiDetailPromptSchemaRegistry": {
                      "treatDangerousAsBreaking": false,
                      "allowBreakingSchemaChanges": false
                    }
                  }
                }
              ],
              "cursor": "cursor-1"
            }
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task List_InteractivePath_UsesPagedTableBranch()
    {
        // arrange
        var page = new ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>(
            [],
            EndCursor: null,
            HasNextPage: false);
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = ApiCommandTestHelper.CreateHost(client, session: null);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "list",
            "--workspace-id",
            "ws-1");

        // assert
        Assert.Equal(0, exitCode);
        client.VerifyAll();
    }
}
