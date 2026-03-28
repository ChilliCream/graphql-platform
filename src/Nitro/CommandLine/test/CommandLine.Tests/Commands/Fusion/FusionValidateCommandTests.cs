using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionValidateCommandTests
{
    [Fact]
    public async Task Validate_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("fusion", "validate");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You need to specify one of: '--source-schema-file' or '--archive'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Validate_WithoutArchiveOrSourceSchemaFiles_ReturnsValidationError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "validate",
            "--api-id",
            "api-1",
            "--stage",
            "prod");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You need to specify one of: '--source-schema-file' or '--archive'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Validate_WithArchiveAndSourceSchemaFiles_ReturnsValidationError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "validate",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--archive",
            "/tmp/archive.far",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You can only specify one of: '--source-schema-file' or '--archive'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Validate_WithMissingArchiveFile_ReturnsError()
    {
        // arrange
        const string archivePath = "/tmp/nitro-fusion-validate-missing.far";
        var fileSystem = new TestFileSystem();
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "validate",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            LOG: Reading file /tmp/nitro-fusion-validate-missing.far
            Validating...
             File /tmp/nitro-fusion-validate-missing.far was not found!
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

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
}
