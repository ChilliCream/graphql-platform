using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class ListApiKeyCommandTests
{
    [Fact]
    public async Task List_MissingWorkspaceIdAndSession_ReturnsError()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var host = ApiKeyCommandTestHelper.CreateHost(client, session: new TestSessionService());

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
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
        var page = new ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>(
            [ApiKeyCommandTestHelper.CreateApiKeyNode("key-1", "first", "Workspace")],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = ApiKeyCommandTestHelper.CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
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
                  "id": "key-1",
                  "name": "first",
                  "workspace": {
                    "name": "Workspace"
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
        var page = new ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>(
            [],
            EndCursor: null,
            HasNextPage: false);
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = ApiKeyCommandTestHelper.CreateHost(client, session: null);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
            "list",
            "--workspace-id",
            "ws-1");

        // assert
        Assert.Equal(0, exitCode);
        client.VerifyAll();
    }
}
