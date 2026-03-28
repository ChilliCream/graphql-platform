using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class PublishMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Publish_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient);

        // act
        var exitCode = await host.InvokeAsync("mcp", "publish");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--stage' is required.
            Option '--mcp-feature-collection-id' is required.
            """);
        mcpClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Publish_FailedUpdate_ReturnsError()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.StartMcpFeatureCollectionPublishAsync(
                "mcp-1",
                "prod",
                "v1",
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMcpFeatureCollectionPublishRequest("publish-1"));

        mcpClient.Setup(x => x.SubscribeToMcpFeatureCollectionPublishAsync(
                "publish-1",
                It.IsAny<CancellationToken>()))
            .Returns(ToPublishUpdates(
                CreateFailedUpdate("Publish failed", "Invalid package")));

        var host = CreateHost(mcpClient);

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "publish",
            "--tag",
            "v1",
            "--stage",
            "prod",
            "--mcp-feature-collection-id",
            "mcp-1");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            LOG: Create publish request
            LOG: Publish request created (ID: publish-1)
            ✕ MCP Feature Collection publish failed

            Publish failed
            Invalid package
            Publishing...
            """);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyAll();
    }

    private static async IAsyncEnumerable<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate>
        ToPublishUpdates(
            params IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection
        CreateMcpFeatureCollectionPublishRequest(string requestId)
    {
        var mock = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>();
        mock.SetupGet(x => x.Id).Returns(requestId);
        return mock.Object;
    }

    private static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_McpFeatureCollectionVersionPublishFailed
        CreateFailedUpdate(params string[] messages)
    {
        var errors = messages
            .Select(message =>
            {
                var error = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors_UnexpectedProcessingError>();
                error.SetupGet(x => x.Message).Returns(message);
                return (IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors)error.Object;
            })
            .ToArray();

        var update = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_McpFeatureCollectionVersionPublishFailed>();
        update.SetupGet(x => x.Errors).Returns(errors);
        return update.Object;
    }

    private static CommandBuilder CreateHost(
        Mock<IMcpClient> mcpClient,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService(mcpClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }
}
