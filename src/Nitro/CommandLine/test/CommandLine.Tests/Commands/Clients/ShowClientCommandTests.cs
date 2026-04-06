using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ShowClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show details of a client.

            Usage:
              nitro client show <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro client show "<client-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "client-1");

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
        SetupInteractionMode(mode);
        ClientsClientMock.Setup(x => x.GetClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowClientCommandQuery_Node?)null);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "client-1");

        // assert
        result.AssertError(
            """
            The client with ID 'client-1' was not found.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithClientId_ReturnSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        ClientsClientMock.Setup(x => x.GetClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShowClientNode("client-1", "web-client", "products"));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "client-1");

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
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSessionWithWorkspace();
        SetupGetClientException(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "client-1");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetClientException(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "client-1");

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSessionWithWorkspace();
        SetupGetClientException(new NitroClientAuthorizationException());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "client-1");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetClientException(new NitroClientAuthorizationException());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            "client-1");

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
    }

    private void SetupGetClientException(Exception ex)
    {
        ClientsClientMock.Setup(x => x.GetClientAsync(
                "client-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
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
}
