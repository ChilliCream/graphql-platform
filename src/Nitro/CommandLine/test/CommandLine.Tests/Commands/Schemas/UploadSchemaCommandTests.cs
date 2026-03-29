using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class UploadSchemaCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "schema",
                "upload",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new schema version

            Usage:
              nitro schema upload [options]

            Options:
              --tag <tag> (REQUIRED)                  The tag of the schema version to deploy [env: NITRO_TAG]
              --schema-file <schema-file> (REQUIRED)  The path to the graphql file with the schema definition [env: NITRO_SCHEMA_FILE]
              --api-id <api-id> (REQUIRED)            The ID of the API [env: NITRO_API_ID]
              --cloud-url <cloud-url>                 The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                     The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                         The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                          Show help and usage information
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
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateUploadExceptionClient(new NitroClientException("upload failed"));
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading schema...
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
        var client = CreateUploadExceptionClient(new NitroClientException("upload failed"));
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading schema...
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
        var client = CreateUploadExceptionClient(new NitroClientException("upload failed"));
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
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
        var client = CreateUploadExceptionClient(new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading schema...
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
        var client = CreateUploadExceptionClient(new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading schema...
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
        var client = CreateUploadExceptionClient(new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IUploadSchema_UploadSchema_Errors mutationError,
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
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading schema...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IUploadSchema_UploadSchema_Errors mutationError,
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
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading schema...
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IUploadSchema_UploadSchema_Errors mutationError,
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
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullSchemaVersion_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IUploadSchema_UploadSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadSchema_UploadSchema_Errors>?)null);
        payload.SetupGet(x => x.SchemaVersion)
            .Returns((IUploadSchema_UploadSchema_SchemaVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading schema...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload schema.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullSchemaVersion_ReturnsError_Interactive()
    {
        // arrange
        var payload = new Mock<IUploadSchema_UploadSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadSchema_UploadSchema_Errors>?)null);
        payload.SetupGet(x => x.SchemaVersion)
            .Returns((IUploadSchema_UploadSchema_SchemaVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading schema...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload schema.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullSchemaVersion_ReturnsError_JsonOutput()
    {
        // arrange
        var payload = new Mock<IUploadSchema_UploadSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadSchema_UploadSchema_Errors>?)null);
        payload.SetupGet(x => x.SchemaVersion)
            .Returns((IUploadSchema_UploadSchema_SchemaVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not upload schema.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsSchema_NonInteractive()
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
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading schema...
            └── ✓ Successfully uploaded schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsSchema_Interactive()
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
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Successfully uploaded schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsSchema_JsonOutput()
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
                "schema",
                "upload",
                "--tag",
                "v1",
                "--schema-file",
                "schema.graphql",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        Assert.Equal("{}", result.StdOut);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    private static Mock<IFileSystem> CreateSchemaFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql"))
            .Returns(new MemoryStream("type Query { hello: String }"u8.ToArray()));
        return fileSystem;
    }

    private static (Mock<ISchemasClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup(
        IUploadSchema_UploadSchema payload)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadSchemaAsync(
                "api-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateSchemaFileSystem();

        return (client, fileSystem);
    }

    private static Mock<ISchemasClient> CreateUploadExceptionClient(Exception ex)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadSchemaAsync(
                "api-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IUploadSchema_UploadSchema CreateUploadSuccessPayload()
    {
        var schemaVersion = new Mock<IUploadSchema_UploadSchema_SchemaVersion>(MockBehavior.Strict);
        schemaVersion.SetupGet(x => x.Id).Returns("sv-1");

        var payload = new Mock<IUploadSchema_UploadSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadSchema_UploadSchema_Errors>?)null);
        payload.SetupGet(x => x.SchemaVersion).Returns(schemaVersion.Object);

        return payload.Object;
    }

    private static IUploadSchema_UploadSchema CreateUploadPayloadWithErrors(
        params IUploadSchema_UploadSchema_Errors[] errors)
    {
        var payload = new Mock<IUploadSchema_UploadSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.SchemaVersion)
            .Returns((IUploadSchema_UploadSchema_SchemaVersion?)null);

        return payload.Object;
    }

    public static IEnumerable<object[]> UploadMutationErrorCases()
    {
        yield return
        [
            new UploadSchema_UploadSchema_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to upload."),
            """
            Not authorized to upload.
            """
        ];

        yield return
        [
            new UploadSchema_UploadSchema_Errors_DuplicatedTagError(
                "DuplicatedTagError",
                "Tag 'v1' already exists."),
            """
            Tag 'v1' already exists.
            """
        ];

        yield return
        [
            new UploadSchema_UploadSchema_Errors_ConcurrentOperationError(
                "ConcurrentOperationError",
                "A concurrent operation is in progress."),
            """
            A concurrent operation is in progress.
            """
        ];

        yield return
        [
            new UploadSchema_UploadSchema_Errors_ApiNotFoundError(
                "ApiNotFoundError",
                "API not found.",
                "api-1"),
            """
            API not found.
            """
        ];

        var unexpectedError = new Mock<IUploadSchema_UploadSchema_Errors>();
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
