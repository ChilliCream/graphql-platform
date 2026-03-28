using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionPublishCommandTests
{
    [Fact]
    public async Task Publish_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("fusion", "publish");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You need to specify one of: '--source-schema', '--source-schema-file', or '--archive'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Publish_WithoutArchiveOrSourceSchemas_ReturnsValidationError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--tag",
            "v1");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You need to specify one of: '--source-schema', '--source-schema-file', or '--archive'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Publish_WithArchiveAndSourceSchema_ReturnsValidationError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--tag",
            "v1",
            "--archive",
            "/tmp/archive.far",
            "--source-schema",
            "accounts");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You can only specify one of: '--source-schema', '--source-schema-file', or '--archive'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Publish_WithMissingArchiveFile_ReturnsError()
    {
        // arrange
        const string archivePath = "/tmp/nitro-fusion-missing.far";
        var fileSystem = new TestFileSystem();
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "publish",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--tag",
            "v1",
            "--archive",
            archivePath);

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Archive file '/tmp/nitro-fusion-missing.far' does not exist.
            """);
        client.VerifyNoOtherCalls();
    }

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
}
