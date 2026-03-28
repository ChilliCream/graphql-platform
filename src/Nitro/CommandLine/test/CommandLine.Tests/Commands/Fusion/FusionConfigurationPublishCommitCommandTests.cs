using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishCommitCommandTests
{
    [Fact]
    public async Task PublishCommit_MissingArchiveOption_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("fusion", "publish", "commit");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--archive' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishCommit_MissingRequestIdAndCache_ReturnsError()
    {
        // arrange
        var archivePath = CreateFilePath(".far");

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var fileSystem = CreateFileSystemWithoutCachedRequestId();
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "commit",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            No request ID was provided and no request ID was found in the cache. Please
            provide a request ID.
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishCommit_WithRequestIdAndMissingArchiveFile_ReturnsError()
    {
        // arrange
        const string archivePath = "/tmp/nitro-fusion-publish-commit-missing.far";
        var fileSystem = new TestFileSystem();
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            "request-1",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            Committing...
             File /tmp/nitro-fusion-publish-commit-missing.far was not found!
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishCommit_WithRequestId_CommitsSuccessfully()
    {
        // arrange
        var archivePath = CreateFilePath(".far");
        var fileSystem = new TestFileSystem(new KeyValuePair<string, string>(archivePath, "archive"));

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.CommitFusionArchiveAsync(
                "request-1",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCommitResult());

        client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                "request-1",
                It.IsAny<CancellationToken>()))
            .Returns(ToEvents(CreatePublishingSuccessEvent()));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            "request-1",
            "--archive",
            archivePath);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
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

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess
        CreatePublishingSuccessEvent()
        => Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess>();

    private static ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish CreateCommitResult()
    {
        var result = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>();
        result.SetupGet(x => x.Errors).Returns([]);
        return result.Object;
    }

    private static TestFileSystem CreateFileSystemWithoutCachedRequestId() => new();

    private static CommandTestHost CreateHost(
        Mock<IFusionConfigurationClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IFusionConfigurationClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        if (fileSystem is not null)
        {
            host.AddService<IFileSystem>(fileSystem);
        }

        return host;
    }

    private static string CreateFilePath(string extension)
        => Path.Combine("/tmp", Path.GetRandomFileName() + extension);
}
