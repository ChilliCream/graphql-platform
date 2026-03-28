using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishBeginCommandTests
{
    [Fact]
    public async Task PublishBegin_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var fileSystem = new TestFileSystem();
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync("fusion", "publish", "begin");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--stage' is required.
            Option '--api-id' is required.
            """);
        client.VerifyNoOtherCalls();
        Assert.Empty(fileSystem.Files);
    }

    [Fact]
    public async Task PublishBegin_ReadyEvent_StoresRequestIdAndReturnsSuccess()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                "api-1",
                "prod",
                "v1",
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeploymentSlotRequestResult("request-1"));

        client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                "request-1",
                It.IsAny<CancellationToken>()))
            .Returns(ToEvents(CreateProcessingTaskIsReadyEvent()));

        var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
        var fileSystem = new TestFileSystem();

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--tag",
            "v1",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
        Assert.Equal("request-1", await fileSystem.ReadAllTextAsync(cacheFile, CancellationToken.None));
    }

    private static async IAsyncEnumerable<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>
        ToEvents(
            params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] events)
    {
        foreach (var @event in events)
        {
            yield return @event;
            await Task.Yield();
        }
    }

    private static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish CreateDeploymentSlotRequestResult(string requestId)
    {
        var mock = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>();
        mock.SetupGet(x => x.RequestId).Returns(requestId);
        return mock.Object;
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady
        CreateProcessingTaskIsReadyEvent()
        => Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady>();

    private static CommandBuilder CreateHost(
        Mock<IFusionConfigurationClient> client,
        TestFileSystem fileSystem,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IFusionConfigurationClient>(client.Object)
            .AddService<IFileSystem>(fileSystem)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }
}
