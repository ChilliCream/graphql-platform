using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class CreateClientCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "create",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Creates a new client

            Usage:
              nitro client create [options]

            Options:
              --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
              --name <name>            The name of the API key (for later reference) [env: NITRO_API_KEY_NAME]
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
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_ReturnSuccess()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        apisClient.Setup(x => x.SelectApisAsync(
                "workspace-from-session",
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>(
                [
                    new SelectApiPromptQuery_WorkspaceById_Apis_Edges_Node_Api(
                        "api-1",
                        "products",
                        [],
                        null,
                        new ShowApiCommandQuery_Node_Settings_ApiSettings(
                            new ShowApiCommandQuery_Node_Settings_SchemaRegistry_SchemaRegistrySettings(false, false)))
                ],
                null,
                false));

        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPayload(CreateClientNode("client-1", "web-client", "products"), errors: null));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "create")
            .Start();

        // act
        command.SelectOption(0);
        command.Input("web-client");
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("For which API do you want to create a client?", result.StdOut);
        Assert.Contains("? Name web-client", result.StdOut);
        Assert.Contains("Successfully created client!", result.StdOut);
        Assert.Contains("\"id\": \"client-1\"", result.StdOut);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_AndName_ReturnSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPayload(CreateClientNode("client-1", "web-client", "products"), errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("Creating client...", result.StdOut);
        Assert.Contains("Successfully created client", result.StdOut);
        Assert.Contains("\"id\": \"client-1\"", result.StdOut);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_AndName_ReturnSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPayload(CreateClientNode("client-1", "web-client", "products"), errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "client-1",
              "name": "web-client",
              "api": {
                "name": "products"
              }
            }
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsApiNotFoundError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        var apiNotFound = new Mock<ICreateClientCommandMutation_CreateClient_Errors_ApiNotFoundError>(MockBehavior.Strict);
        apiNotFound.As<IApiNotFoundError>().SetupGet(x => x.Message).Returns("The API was not found.");

        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPayload(client: null, errors: [apiNotFound.Object]));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The API was not found.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsUnauthorizedOperationError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        var unauthorized = new Mock<ICreateClientCommandMutation_CreateClient_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        unauthorized.As<IUnauthorizedOperation>().SetupGet(x => x.Message).Returns("Unauthorized operation.");

        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPayload(client: null, errors: [unauthorized.Object]));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Unauthorized operation.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsGenericError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        var genericError = new Mock<ICreateClientCommandMutation_CreateClient_Errors>(MockBehavior.Strict);
        genericError.As<IError>().SetupGet(x => x.Message).Returns("something bad happened");

        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPayload(client: null, errors: [genericError.Object]));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Unexpected mutation error: something bad happened
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsNoClient_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPayload(client: null, errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not create client.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateExceptionClient(new NitroClientException("create failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: create failed
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "web-client")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    private static Mock<IClientsClient> CreateExceptionClient(Exception ex)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static ICreateClientCommandMutation_CreateClient CreateClientPayload(
        ICreateClientCommandMutation_CreateClient_Client? client,
        IReadOnlyList<ICreateClientCommandMutation_CreateClient_Errors>? errors)
    {
        var payload = new Mock<ICreateClientCommandMutation_CreateClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Client).Returns(client);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    private static ICreateClientCommandMutation_CreateClient_Client CreateClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns(["products"]);

        var clientNode = new Mock<ICreateClientCommandMutation_CreateClient_Client>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
    }
}
