using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class UploadClientCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "upload",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new client version

            Usage:
              nitro client upload [options]

            Options:
              --tag <tag> (REQUIRED)                          The tag of the schema version to deploy [env: NITRO_TAG]
              --operations-file <operations-file> (REQUIRED)  The path to the json file with the operations [env: NITRO_OPERATIONS_FILE]
              --client-id <client-id> (REQUIRED)              The ID of the client [env: NITRO_CLIENT_ID]
              --cloud-url <cloud-url>                         The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                             The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                 The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                  Show help and usage information
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
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
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
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version for client 'client-1'...
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
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading new version for client 'client-1'...
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
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
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
        var client = CreateUploadExceptionClient(
            new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version for client 'client-1'...
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
        var client = CreateUploadExceptionClient(
            new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading new version for client 'client-1'...
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
        var client = CreateUploadExceptionClient(
            new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
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
        IUploadClient_UploadClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(
            CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version for client 'client-1'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IUploadClient_UploadClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(
            CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading new version for client 'client-1'...
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IUploadClient_UploadClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(
            CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullClientVersion_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadClient_UploadClient_Errors>?)null);
        payload.SetupGet(x => x.ClientVersion)
            .Returns((IUploadClient_UploadClient_ClientVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version for client 'client-1'...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload client.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullClientVersion_ReturnsError_Interactive()
    {
        // arrange
        var payload = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadClient_UploadClient_Errors>?)null);
        payload.SetupGet(x => x.ClientVersion)
            .Returns((IUploadClient_UploadClient_ClientVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Uploading new version for client 'client-1'...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload client.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullClientVersion_ReturnsError_JsonOutput()
    {
        // arrange
        var payload = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadClient_UploadClient_Errors>?)null);
        payload.SetupGet(x => x.ClientVersion)
            .Returns((IUploadClient_UploadClient_ClientVersion?)null);

        var (client, fileSystem) = CreateUploadSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not upload client.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsClient_NonInteractive()
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
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version for client 'client-1'...
            └── ✓ Successfully uploaded operations!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsClient_Interactive()
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
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Successfully uploaded operations!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsClient_JsonOutput()
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
                "client",
                "upload",
                "--tag",
                "v1",
                "--operations-file",
                "operations.json",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    private static Mock<IFileSystem> CreateOperationsFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream("operations.json"))
            .Returns(new MemoryStream("{}"u8.ToArray()));
        return fileSystem;
    }

    private static IUploadClient_UploadClient CreateUploadSuccessPayload()
    {
        var clientVersion = new Mock<IUploadClient_UploadClient_ClientVersion>(MockBehavior.Strict);
        clientVersion.SetupGet(x => x.Id).Returns("cv-1");

        var payload = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadClient_UploadClient_Errors>?)null);
        payload.SetupGet(x => x.ClientVersion).Returns(clientVersion.Object);

        return payload.Object;
    }

    private static IUploadClient_UploadClient CreateUploadPayloadWithErrors(
        params IUploadClient_UploadClient_Errors[] errors)
    {
        var payload = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.ClientVersion)
            .Returns((IUploadClient_UploadClient_ClientVersion?)null);

        return payload.Object;
    }

    private static (Mock<IClientsClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup(
        IUploadClient_UploadClient payload)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadClientVersionAsync(
                "client-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateOperationsFileSystem();

        return (client, fileSystem);
    }

    private static Mock<IClientsClient> CreateUploadExceptionClient(Exception ex)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadClientVersionAsync(
                "client-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    public static IEnumerable<object[]> UploadMutationErrorCases()
    {
        yield return
        [
            new UploadClient_UploadClient_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to upload."),
            """
            Not authorized to upload.
            """
        ];

        yield return
        [
            new UploadClient_UploadClient_Errors_ClientNotFoundError(
                "Client not found.",
                "client-1"),
            """
            Client not found.
            """
        ];

        yield return
        [
            new UploadClient_UploadClient_Errors_DuplicatedTagError(
                "DuplicatedTagError",
                "Tag 'v1' already exists."),
            """
            Tag 'v1' already exists.
            """
        ];

        yield return
        [
            new UploadClient_UploadClient_Errors_ConcurrentOperationError(
                "ConcurrentOperationError",
                "A concurrent operation is in progress."),
            """
            A concurrent operation is in progress.
            """
        ];

        yield return
        [
            new UploadClient_UploadClient_Errors_InvalidPersistedQueryError(
                "Invalid persisted query."),
            """
            Invalid persisted query.
            """
        ];

        yield return
        [
            new UploadClient_UploadClient_Errors_InvalidSourceMetadataInputError(
                "Invalid source metadata."),
            """
            Invalid source metadata.
            """
        ];

        var unexpectedError = new Mock<IUploadClient_UploadClient_Errors>();
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
