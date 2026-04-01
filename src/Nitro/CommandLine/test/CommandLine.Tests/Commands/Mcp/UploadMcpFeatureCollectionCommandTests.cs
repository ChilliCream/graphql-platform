using System.Net;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class UploadMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "mcp",
                "upload",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new MCP feature collection version.
            
            Usage:
              nitro mcp upload [options]
            
            Options:
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              --tag <tag> (REQUIRED)                                              The tag of the schema version to deploy [env: NITRO_TAG]
              -p, --prompt-pattern <prompt-pattern>                               One or more file patterns to locate MCP prompt definition files (*.json)
              -t, --tool-pattern <tool-pattern>                                   One or more file patterns to locate MCP tool definition files (*.graphql)
              --cloud-url <cloud-url>                                             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
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
        var result = await new CommandBuilder(fixture)
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
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "upload")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--mcp-feature-collection-id' is required.
            Option '--tag' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoFilesFound_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        var fileSystem = CreateEmptyFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
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
            Could not find any MCP prompt or tool definition files with the provided
            patterns.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
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
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
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
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
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
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
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
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsRequestEntityTooLarge_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientHttpRequestException(HttpStatusCode.RequestEntityTooLarge));

        // act
        var result = await new CommandBuilder(fixture)
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
            The server returned a 413 (Request Entity Too Large) HTTP status code. If you
            are running a self-hosted instance, check your ingress controller body-size
            limits, reverse proxy settings, or load balancer request size limits.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsRequestEntityTooLarge_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientHttpRequestException(HttpStatusCode.RequestEntityTooLarge));

        // act
        var result = await new CommandBuilder(fixture)
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
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned a 413 (Request Entity Too Large) HTTP status code. If you
            are running a self-hosted instance, check your ingress controller body-size
            limits, reverse proxy settings, or load balancer request size limits.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCasesNonInteractive))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
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
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        InteractionMode mode,
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
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
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullVersion_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithNullVersion());

        // act
        var result = await new CommandBuilder(fixture)
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
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
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
        var result = await new CommandBuilder(fixture)
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
        result.AssertSuccess(
            """
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✓ Uploaded new MCP feature collection version 'v1'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
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
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
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
        result.AssertSuccess(
            """
            {}
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Upload_Should_ReturnError_When_SourceMetadataInvalid()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "upload",
                "--tag",
                "v1",
                "--mcp-feature-collection-id",
                "mcp-1",
                "--source-metadata",
                "{broken}")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to parse --source-metadata: 'b' is an invalid start of a property name.
            Expected a '"'. Path: $ | LineNumber: 0 | BytePositionInLine: 1.
            """);
        Assert.Equal(1, result.ExitCode);
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
        var modes = new[] { InteractionMode.Interactive, InteractionMode.JsonOutput };

        foreach (var mode in modes)
        {
            yield return
            [
                mode,
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                    "mcp-1", "MCP Feature Collection not found."),
                """
                MCP Feature Collection not found.
                """
            ];

            yield return
            [
                mode,
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation", "Not authorized to upload."),
                """
                Not authorized to upload.
                """
            ];

            yield return
            [
                mode,
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_DuplicatedTagError(
                    "DuplicatedTagError", "Tag 'v1' already exists."),
                """
                Tag 'v1' already exists.
                """
            ];

            yield return
            [
                mode,
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_ConcurrentOperationError(
                    "ConcurrentOperationError", "A concurrent operation is in progress."),
                """
                A concurrent operation is in progress.
                """
            ];

            yield return
            [
                mode,
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_InvalidMcpFeatureCollectionArchiveError(
                    "Invalid archive format."),
                """
                The server received an invalid archive. This indicates a bug in the tooling.
                Please notify ChilliCream.
                Error received: Invalid archive format.
                """
            ];

            var unexpectedError = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors>();
            unexpectedError
                .As<IError>()
                .SetupGet(x => x.Message)
                .Returns("Something went wrong.");

            yield return
            [
                mode,
                unexpectedError.Object,
                """
                Unexpected mutation error: Something went wrong.
                """
            ];
        }
    }

    public static IEnumerable<object[]> UploadMutationErrorCasesNonInteractive()
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
            The server received an invalid archive. This indicates a bug in the tooling.
            Please notify ChilliCream.
            Error received: Invalid archive format.
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
