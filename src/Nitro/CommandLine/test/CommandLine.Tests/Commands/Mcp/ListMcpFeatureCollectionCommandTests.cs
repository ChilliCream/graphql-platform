using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class ListMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task List_MissingApiId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "list",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            The API ID is required in non-interactive mode.
            """);
        mcpClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var page = new ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node>(
            [CreateCollectionNode("mcp-1", "feature-collection")],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "list",
            "--api-id",
            "api-1",
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
                  "id": "mcp-1",
                  "name": "feature-collection"
                }
              ],
              "cursor": "cursor-1"
            }
            """);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyAll();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_InteractivePath_UsesPagedTableBranch()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node>(
                [],
                EndCursor: null,
                HasNextPage: false));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient, apisClient);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "list",
            "--api-id",
            "api-1");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyAll();
        apisClient.VerifyNoOtherCalls();
    }

    private static CommandBuilder CreateHost(
        Mock<IMcpClient> mcpClient,
        Mock<IApisClient> apisClient,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService(mcpClient.Object)
            .AddService(apisClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node_McpFeatureCollection
        CreateCollectionNode(
            string id,
            string name)
    {
        var collection = new Mock<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node_McpFeatureCollection>();
        collection.SetupGet(x => x.Id).Returns(id);
        collection.SetupGet(x => x.Name).Returns(name);

        return collection.Object;
    }
}
