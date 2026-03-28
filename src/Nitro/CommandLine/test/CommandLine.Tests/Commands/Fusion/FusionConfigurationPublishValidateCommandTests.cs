using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishValidateCommandTests
{
    [Fact]
    public async Task PublishValidate_MissingArchiveOption_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var fileSystem = new TestFileSystem();
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync("fusion", "publish", "validate");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--archive' is required.
            """);
        client.VerifyNoOtherCalls();
        Assert.Empty(fileSystem.Files);
    }

    [Fact]
    public async Task PublishValidate_MissingRequestIdAndCache_ReturnsError()
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
            "validate",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            No request ID was provided and no request ID was found in the cache. Please provide a request ID.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishValidate_WithRequestIdAndMissingArchiveFile_ReturnsError()
    {
        // arrange
        const string archivePath = "/tmp/nitro-fusion-publish-validate-missing.far";
        var fileSystem = new TestFileSystem();
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "validate",
            "--request-id",
            "request-1",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            LOG: Initialized
            Validating...
            """);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            [red] File /tmp/nitro-fusion-publish-validate-missing.far was not found![/]
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishValidate_WithRequestId_ValidationSuccess_ReturnsSuccess()
    {
        // arrange
        var archivePath = CreateFilePath(".far");
        var fileSystem = new TestFileSystem(new KeyValuePair<string, string>(archivePath, "archive"));

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateFusionConfigurationPublishAsync(
                "request-1",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition>());

        client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                "request-1",
                It.IsAny<CancellationToken>()))
            .Returns(ToEvents(CreateValidationSuccessEvent()));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "validate",
            "--request-id",
            "request-1",
            "--archive",
            archivePath);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static async IAsyncEnumerable<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged> ToEvents(
        params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] events)
    {
        foreach (var @event in events)
        {
            yield return @event;
            await Task.Yield();
        }
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged CreateValidationSuccessEvent()
        => new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationSuccess(
            ProcessingState.Success,
            "FusionConfigurationValidationSuccess",
            "FusionConfigurationValidationSuccess",
            []);

    private static TestFileSystem CreateFileSystemWithoutCachedRequestId() => new();

    private static CommandBuilder CreateHost(
        Mock<IFusionConfigurationClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
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
