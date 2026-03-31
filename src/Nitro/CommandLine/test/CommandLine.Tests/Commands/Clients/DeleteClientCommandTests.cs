using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class DeleteClientCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "client",
                "delete",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete a client.

            Usage:
              nitro client delete [<id>] [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
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
        var result = await new CommandBuilder(fixture)
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
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
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
        var result = await new CommandBuilder(fixture)
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
        var result = await new CommandBuilder(fixture)
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
            Deleting client 'client-1'
            └── ✓ Deleted client 'client-1'.

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

        var command = new CommandBuilder(fixture)
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
        var result = await new CommandBuilder(fixture)
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
        var result = await new CommandBuilder(fixture)
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
        var result = await new CommandBuilder(fixture)
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
        var result = await new CommandBuilder(fixture)
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
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
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
        var result = await new CommandBuilder(fixture)
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
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
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
        var result = await new CommandBuilder(fixture)
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
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
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
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
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
        var clientsClient = CreateDeleteExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
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
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
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
        var clientsClient = CreateDeleteExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
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
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateDeleteExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
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
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
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
        var clientsClient = CreateDeleteExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
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
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
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
        var clientsClient = CreateDeleteExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
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
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
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
