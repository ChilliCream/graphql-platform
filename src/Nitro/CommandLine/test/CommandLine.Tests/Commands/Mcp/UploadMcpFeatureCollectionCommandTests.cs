using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class UploadMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "mcp",
                "upload",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new MCP Feature Collection version

            Usage:
              nitro mcp upload [options]

            Options:
              --tag <tag> (REQUIRED)                                              The tag of the schema version to deploy [env: NITRO_TAG]
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              -p, --prompt-pattern <prompt-pattern>                               One or more file patterns to locate MCP prompt definition files (*.json).
              -t, --tool-pattern <tool-pattern>                                   One or more file patterns to locate MCP tool definition files (*.graphql).
              --cloud-url <cloud-url>                                             The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                                     The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                      Show help and usage information
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
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task NoFilesFound_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        var fileSystem = CreateEmptyFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not find any MCP prompt or tool definition files with the provided patterns.
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientException("upload failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: upload failed
            """);
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
        var (client, fileSystem) = CreateUploadSetup(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new MCP Feature Collection version...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading new MCP Feature Collection version...
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullVersion_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithNullVersion());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new MCP Feature Collection version...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload MCP Feature Collection version.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new MCP Feature Collection version...
            └── ✓ Successfully uploaded new MCP Feature Collection version!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Successfully uploaded new MCP Feature Collection version!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    private static Mock<IFileSystem> CreateMcpFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GlobMatch(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns(["prompt.mcp-prompt.json"]);
        fileSystem.Setup(x => x.OpenReadStream("prompt.mcp-prompt.json"))
            .Returns(new MemoryStream("{}"u8.ToArray()));
        return fileSystem;
    }

    private static Mock<IFileSystem> CreateEmptyFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GlobMatch(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns([]);
        return fileSystem;
    }

    private static (Mock<IMcpClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup(
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection payload)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadMcpFeatureCollectionVersionAsync(
                "mcp-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateMcpFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IMcpClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup(
        Exception ex)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadMcpFeatureCollectionVersionAsync(
                "mcp-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = CreateMcpFileSystem();

        return (client, fileSystem);
    }

    private static IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection CreateUploadSuccessPayload()
    {
        var version = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_McpFeatureCollectionVersion>(MockBehavior.Strict);
        version.SetupGet(x => x.Id).Returns("mcpv-1");

        var payload = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.McpFeatureCollectionVersion).Returns(version.Object);

        return payload.Object;
    }

    private static IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection CreateUploadPayloadWithNullVersion()
    {
        var payload = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.McpFeatureCollectionVersion)
            .Returns((IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_McpFeatureCollectionVersion?)null);

        return payload.Object;
    }

    private static IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection CreateUploadPayloadWithErrors(
        params IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.McpFeatureCollectionVersion)
            .Returns((IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_McpFeatureCollectionVersion?)null);

        return payload.Object;
    }

    public static IEnumerable<object[]> UploadMutationErrorCases()
    {
        yield return
        [
            new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                "mcp-1", "MCP Feature Collection not found."),
            """
            MCP Feature Collection not found.
            """
        ];

        yield return
        [
            new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_UnauthorizedOperation(
                "UnauthorizedOperation", "Not authorized to upload."),
            """
            Not authorized to upload.
            """
        ];

        yield return
        [
            new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_DuplicatedTagError(
                "DuplicatedTagError", "Tag 'v1' already exists."),
            """
            Tag 'v1' already exists.
            """
        ];

        yield return
        [
            new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_ConcurrentOperationError(
                "ConcurrentOperationError", "A concurrent operation is in progress."),
            """
            A concurrent operation is in progress.
            """
        ];

        yield return
        [
            new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_InvalidMcpFeatureCollectionArchiveError(
                "Invalid archive format."),
            """
            Invalid archive format.
            """
        ];

        var unexpectedError = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        yield return
        [
            unexpectedError.Object,
            """
            Unexpected mutation error: Something went wrong.
            """
        ];
    }
}
