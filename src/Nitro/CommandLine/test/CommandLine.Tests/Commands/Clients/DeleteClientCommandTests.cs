using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class DeleteClientCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "delete",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Deletes a client

            Usage:
              nitro client delete [<id>] [options]

            Arguments:
              <id>  The ID

            Options:
              --force                  Will not ask for confirmation on deletes or overwrites.
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information
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
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingClientId_NonInteractive_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "delete")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The 'id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_AndForce_ReturnSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeletePayload(CreateClientNode("client-1", "web-client", "products"), errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ? Which client do you want to delete?: client-1
            Deleting client...
            └── ✓ Successfully deleted client!

            {
              "id": "client-1",
              "name": "web-client",
              "api": {
                "name": "products"
              }
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_AndNoForce_UserCancels_ReturnSuccess()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "delete",
                "client-1")
            .Start();

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ? Which client do you want to delete?: client-1
            ? Do you want to delete the client with ID client-1? [y/n] (y): n
            ✓ Aborted.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsClientNotFoundError_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        var notFound = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById_Errors_ClientNotFoundError>(
            MockBehavior.Strict);
        notFound.As<IClientNotFoundError>().SetupGet(x => x.Message).Returns("Client not found.");

        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeletePayload(client: null, errors: [notFound.Object]));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Client not found.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsClientNotFoundError_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        var notFound = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById_Errors_ClientNotFoundError>(
            MockBehavior.Strict);
        notFound.As<IClientNotFoundError>().SetupGet(x => x.Message).Returns("Client not found.");

        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeletePayload(client: null, errors: [notFound.Object]));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Client not found.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsClientNotFoundError_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        var notFound = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById_Errors_ClientNotFoundError>(
            MockBehavior.Strict);
        notFound.As<IClientNotFoundError>().SetupGet(x => x.Message).Returns("Client not found.");

        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeletePayload(client: null, errors: [notFound.Object]));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Client not found.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNoClient_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeletePayload(client: null, errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not delete the client.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNoClient_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeletePayload(client: null, errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not delete the client.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNoClient_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeletePayload(client: null, errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not delete the client.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientException("delete failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: delete failed
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientException("delete failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: delete failed
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientException("delete failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: delete failed
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "delete",
                "client-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    private static Mock<IClientsClient> CreateDeleteExceptionClient(Exception ex)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IDeleteClientByIdCommandMutation_DeleteClientById CreateDeletePayload(
        IDeleteClientByIdCommandMutation_DeleteClientById_Client? client,
        IReadOnlyList<IDeleteClientByIdCommandMutation_DeleteClientById_Errors>? errors)
    {
        var payload = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById>(MockBehavior.Strict);
        payload.SetupGet(x => x.Client).Returns(client);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    private static IDeleteClientByIdCommandMutation_DeleteClientById_Client CreateClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns(["products"]);

        var clientNode = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById_Client>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
    }
}
