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
            Uploading operations...
            LOG: Initialized
            LOG: Reading file operations.json
            LOG: Uploading client...
            └── Failed!
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
            LOG: Initialized
            LOG: Reading file operations.json
            LOG: Uploading client...

            [    ] Uploading operations...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: upload failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateUploadExceptionClient(new NitroClientAuthorizationException("forbidden"));
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
            Uploading operations...
            LOG: Initialized
            LOG: Reading file operations.json
            LOG: Uploading client...
            └── Failed!
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
            LOG: Initialized
            LOG: Reading file operations.json
            LOG: Uploading client...

            [    ] Uploading operations...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsClient_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup();

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
            Uploading operations...
            LOG: Initialized
            LOG: Reading file operations.json
            LOG: Uploading client...
            Successfully uploaded operations!
            └── Failed!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_UploadsClient_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup();

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
            LOG: Initialized
            LOG: Reading file operations.json
            LOG: Uploading client...
            Successfully uploaded operations!

            [    ] Uploading operations...
            """);
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

    private static (Mock<IClientsClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup()
    {
        var uploadResult = new Mock<IUploadClient_UploadClient>(MockBehavior.Strict);

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadClientVersionAsync(
                "client-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult.Object);

        var fileSystem = CreateOperationsFileSystem();

        return (client, fileSystem);
    }

    private static Mock<IClientsClient> CreateUploadExceptionClient(Exception ex)
    {
        var fileSystem = CreateOperationsFileSystem();

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
}
