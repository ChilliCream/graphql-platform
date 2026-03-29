using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionPublishCommandTests
{
    private const string DefaultApiId = "api-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1";
    private const string DefaultArchiveFile = "fusion.far";
    private const string DefaultRequestId = "request-123";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "publish",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publishes a Fusion archive to Nitro.
              To take control over the deployment orchestration use sub-commands like 'begin'.
              If you don't specify --archive and instead use --source-schema-identifiers or --schema-files, a Fusion v2 composition will be performed internally.
              The orchestration sub-commands can be used for both Fusion v1 and v2.

            Usage:
              nitro fusion publish [command] [options]

            Options:
              --api-id <api-id> (REQUIRED)                                              The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)                                                The name of the stage [env: NITRO_STAGE]
              --tag <tag> (REQUIRED)                                                    The tag of the schema version to deploy [env: NITRO_TAG]
              --source-schema-identifiers <source-schema-identifiers>                   The list of source schema identifiers like subgraph1@v1 subgraph2@v2 [env: NITRO_SOURCE_SCHEMA_IDENTIFIERS]
              --schema-files <schema-files>                                             The list of source schema files [env: NITRO_SCHEMA_FILES]
              --archive <archive>                                                       The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version) [env: FUSION_CONFIG_FILE]
              -w, --working-directory <working-directory>                                Sets the working directory for the command. [default: /Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/test/CommandLine.Tests/bin/Debug/net9.0]
              --source-metadata <source-metadata>                                       Optional JSON-formatted metadata about the source [env: NITRO_SOURCE_METADATA]
              --cloud-url <cloud-url>                                                   The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                       The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                                           The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                            Show help and usage information

            Commands:
              begin       Begin a configuration publish. This command will request a deployment slot
              start       Start a Fusion configuration publish
              validate    Validates a Fusion configuration against the schema and clients.
              cancel      Cancel a Fusion configuration publish
              commit      Commit a Fusion configuration publish
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag)
            .ExecuteAsync();

        // assert
        Assert.Contains("You need to specify one of", result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MultipleExclusiveOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile,
                "--schema-files",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        Assert.Contains("You can only specify one of", result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ArchiveFileDoesNotExist_ReturnsError_NonInteractive()
    {
        // arrange
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists(DefaultArchiveFile))
            .Returns(false);

        // act
        var result = await new CommandBuilder()
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        Assert.Contains($"Archive file '{DefaultArchiveFile}' does not exist.", result.StdErr);
        Assert.Equal(1, result.ExitCode);

        fileSystem.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishExceptionSetup(
            new NitroClientException("request failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        Assert.Contains("request failed", result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishExceptionSetup(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        Assert.Contains(
            "The server rejected your request as unauthorized.",
            result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateArchivePublishExceptionSetup(Exception ex)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                DefaultApiId,
                DefaultStage,
                DefaultTag,
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists(DefaultArchiveFile))
            .Returns(true);
        fileSystem.Setup(x => x.OpenReadStream(DefaultArchiveFile))
            .Returns(new MemoryStream("archive-content"u8.ToArray()));

        return (client, fileSystem);
    }
}
