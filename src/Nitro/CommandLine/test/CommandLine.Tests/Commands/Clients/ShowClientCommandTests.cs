using ChilliCream.Nitro.Client;
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
            ClientId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
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
        SetupShowClientQuery(null);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            ClientId);

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
        SetupShowClientQuery(CreateShowClientNode(ClientId, ClientName, ApiName));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            ClientId);

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
    public async Task ShowClientThrows_ReturnsError()
    {
        // arrange
        SetupShowClientQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "show",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    private static IShowClientCommandQuery_Node CreateShowClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([apiName]);

        var clientNode = new Mock<IShowClientCommandQuery_Node_Client>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
    }
}
