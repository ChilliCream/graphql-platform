using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ShowClientCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "show",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Shows details of a client

            Usage:
              nitro client show <id> [options]

            Arguments:
              <id>  The ID

            Options:
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
                "show",
                "client-1")
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
    public async Task ClientNotFound_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowClientCommandQuery_Node?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "show",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The client with ID 'client-1' was not found.
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task WithClientId_ReturnSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShowClientNode("client-1", "web-client", "products"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "show",
                "client-1")
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

        client.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_ReturnSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShowClientNode("client-1", "web-client", "products"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "show",
                "client-1")
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

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateShowExceptionClient(new NitroClientException("show failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "show",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: show failed
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateShowExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "show",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static IShowClientCommandQuery_Node CreateShowClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns(["products"]);

        var clientNode = new Mock<IShowClientCommandQuery_Node_Client>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
    }

    private static Mock<IClientsClient> CreateShowExceptionClient(Exception ex)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
