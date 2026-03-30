using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionUploadCommandTests
{
    private const string SchemaFilePath = "/tmp/subgraph.graphqls";
    private const string SettingsFilePath = "/tmp/subgraph-settings.json";
    private const string SchemaContent = "type Query { hello: String }";
    private static readonly byte[] _settingsBytes =
        """{"name":"subgraph1"}"""u8.ToArray();

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "upload",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a source schema for a later composition.

            Usage:
              nitro fusion upload [options]

            Options:
              --api-id <api-id> (REQUIRED)                              The ID of the API [env: NITRO_API_ID]
              --tag <tag> (REQUIRED)                                    The tag of the schema version to deploy [env: NITRO_TAG]
              -f, --source-schema-file <source-schema-file> (REQUIRED)  The path to a source schema file (.graphqls) or directory containing a source schema file.
              -w, --working-directory <working-directory>               Sets the working directory for the command.
              --cloud-url <cloud-url>                                   The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                       The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                           The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                            Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task MutationReturnsUnauthorizedOperationError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        error.SetupGet(x => x.Message).Returns("Not authorized to upload.");

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Not authorized to upload.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsUnauthorizedOperationError_ReturnsError_Interactive()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        error.SetupGet(x => x.Message).Returns("Not authorized to upload.");

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading source schema '/tmp/subgraph.graphqls'...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Not authorized to upload.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsUnauthorizedOperationError_ReturnsError_JsonOutput()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        error.SetupGet(x => x.Message).Returns("Not authorized to upload.");

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Not authorized to upload.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsDuplicatedTagError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_DuplicatedTagError>(MockBehavior.Strict);
        error.SetupGet(x => x.Message).Returns("Tag 'v1' already exists.");

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Tag 'v1' already exists.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsConcurrentOperationError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_ConcurrentOperationError>(MockBehavior.Strict);
        error.SetupGet(x => x.Message).Returns("A concurrent operation is in progress.");

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            A concurrent operation is in progress.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsInvalidFusionSourceSchemaArchiveError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_InvalidFusionSourceSchemaArchiveError>(MockBehavior.Strict);
        error.As<IInvalidFusionSourceSchemaArchiveError>().SetupGet(x => x.Message).Returns("The archive is invalid.");
        error.As<IError>().SetupGet(x => x.Message).Returns("The archive is invalid.");

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The archive is invalid.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsUnknownError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>(MockBehavior.Strict);
        error.As<IError>().SetupGet(x => x.Message).Returns("Something went wrong.");

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Unexpected mutation error: Something went wrong.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsCompletelyUnknownError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>(MockBehavior.Strict);

        var (client, fileSystem) = CreateUploadSetup(CreatePayloadWithErrors(error.Object));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Unexpected mutation error.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullFusionSubgraphVersion_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>?)null);
        payload.SetupGet(x => x.FusionSubgraphVersion)
            .Returns((IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Upload of source schema failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullFusionSubgraphVersion_ReturnsError_Interactive()
    {
        // arrange
        var payload = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>?)null);
        payload.SetupGet(x => x.FusionSubgraphVersion)
            .Returns((IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading source schema '/tmp/subgraph.graphqls'...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Upload of source schema failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullFusionSubgraphVersion_ReturnsError_JsonOutput()
    {
        // arrange
        var payload = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>?)null);
        payload.SetupGet(x => x.FusionSubgraphVersion)
            .Returns((IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Upload of source schema failed.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsSourceSchema_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✓ Successfully uploaded source schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsSourceSchema_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Successfully uploaded source schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsSourceSchema_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "tag": "v1"
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientException("upload failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: upload failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientException("upload failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading source schema '/tmp/subgraph.graphqls'...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: upload failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientException("upload failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: upload failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading source schema '/tmp/subgraph.graphqls'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading source schema '/tmp/subgraph.graphqls'...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "upload",
                "--api-id",
                "api-1",
                "--tag",
                "v1",
                "--source-schema-file",
                SchemaFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static Mock<IFileSystem> CreateSchemaFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        fileSystem.Setup(x => x.GetCurrentDirectory())
            .Returns("/tmp");

        // ReadSourceSchemaAsync: check if path is directory (no), then check if file (yes)
        fileSystem.Setup(x => x.DirectoryExists(SchemaFilePath))
            .Returns(false);
        fileSystem.Setup(x => x.FileExists(SchemaFilePath))
            .Returns(true);

        // ReadSourceSchemaAsync: read settings JSON
        fileSystem.Setup(x => x.FileExists(SettingsFilePath))
            .Returns(true);
        fileSystem.Setup(x => x.ReadAllBytesAsync(SettingsFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_settingsBytes);

        // ReadSourceSchemaAsync: read schema text
        fileSystem.Setup(x => x.ReadAllTextAsync(SchemaFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SchemaContent);

        return fileSystem;
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup(
        IUploadFusionSubgraph_UploadFusionSubgraph payload)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadFusionSubgraphAsync(
                "api-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateSchemaFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateExceptionSetup(
        Exception ex)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadFusionSubgraphAsync(
                "api-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = CreateSchemaFileSystem();

        return (client, fileSystem);
    }

    private static IUploadFusionSubgraph_UploadFusionSubgraph CreateSuccessPayload()
    {
        var fusionSubgraphVersion = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion>(MockBehavior.Strict);
        fusionSubgraphVersion.SetupGet(x => x.Id).Returns("fsv-1");

        var payload = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>?)null);
        payload.SetupGet(x => x.FusionSubgraphVersion).Returns(fusionSubgraphVersion.Object);

        return payload.Object;
    }

    private static IUploadFusionSubgraph_UploadFusionSubgraph CreatePayloadWithErrors(
        params IUploadFusionSubgraph_UploadFusionSubgraph_Errors[] errors)
    {
        var payload = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.FusionSubgraphVersion)
            .Returns((IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion?)null);

        return payload.Object;
    }
}
