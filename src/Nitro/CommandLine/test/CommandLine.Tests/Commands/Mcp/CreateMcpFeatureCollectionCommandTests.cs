using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class CreateMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Create_MissingWorkspaceAndApi_ReturnsError()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient, apisClient, NoSession());

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "create",
            "--name",
            "feature-collection",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            You are not logged in. Run `nitro login` to sign in or specify the workspace ID
            with the --workspace-id option (if available).
            """);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithApiIdAndName_JsonOutput_ReturnsCollection()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "feature-collection",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCollectionResult("mcp-1", "feature-collection"));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "create",
            "--api-id",
            "api-1",
            "--name",
            "feature-collection",
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
        apisClient.VerifyNoOtherCalls();
    }

    private static CommandTestHost CreateHost(
        Mock<IMcpClient> mcpClient,
        Mock<IApisClient> apisClient,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(mcpClient.Object)
            .AddService(apisClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static TestSessionService NoSession() => new();

    private static ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection CreateCollectionResult(
        string id,
        string name)
    {
        var collection = new Mock<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_McpFeatureCollection_McpFeatureCollection>();
        collection.SetupGet(x => x.Id).Returns(id);
        collection.SetupGet(x => x.Name).Returns(name);

        var payload = new Mock<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection>();
        payload.SetupGet(x => x.McpFeatureCollection).Returns(collection.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
