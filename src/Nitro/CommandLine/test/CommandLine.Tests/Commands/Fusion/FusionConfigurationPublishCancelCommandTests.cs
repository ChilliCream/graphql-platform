using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishCancelCommandTests
{
    [Fact]
    public async Task PublishCancel_MissingRequestIdAndCache_ReturnsError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var fileSystem = CreateFileSystemWithoutCachedRequestId();
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync("fusion", "publish", "cancel");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            No request ID was provided and no request ID was found in the cache. Please
            provide a request ID.
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
        Assert.Empty(fileSystem.Files);
    }

    [Fact]
    public async Task PublishCancel_WithRequestId_CancelsRequest()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ReleaseDeploymentSlotAsync(
                "request-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition>());

        var fileSystem = new TestFileSystem();
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "cancel",
            "--request-id",
            "request-1");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
        Assert.Empty(fileSystem.Files);
    }

    private static TestFileSystem CreateFileSystemWithoutCachedRequestId()
    {
        return new TestFileSystem();
    }

    private static CommandTestHost CreateHost(
        Mock<IFusionConfigurationClient> client,
        TestFileSystem fileSystem,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IFusionConfigurationClient>(client.Object)
            .AddService<IFileSystem>(fileSystem)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }
}
