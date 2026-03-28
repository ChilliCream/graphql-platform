using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class DeleteMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Delete_MissingId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient);

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "delete",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            The MCP Feature Collection ID is required in non-interactive mode.
            """);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_WithIdAndForce_JsonOutput_ReturnsDeletedCollection()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync("mcp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCollectionResult("mcp-1", "feature-collection"));

        var host = CreateHost(mcpClient);

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "delete",
            "mcp-1",
            "--force",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "mcp-1",
              "name": "feature-collection"
            }
            """);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IMcpClient> mcpClient,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(mcpClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById CreateCollectionResult(
        string id,
        string name)
    {
        var collection = new Mock<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_McpFeatureCollection_McpFeatureCollection>();
        collection.SetupGet(x => x.Id).Returns(id);
        collection.SetupGet(x => x.Name).Returns(name);

        var payload = new Mock<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById>();
        payload.SetupGet(x => x.McpFeatureCollection).Returns(collection.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
