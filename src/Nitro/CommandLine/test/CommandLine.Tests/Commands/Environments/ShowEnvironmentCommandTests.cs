using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class ShowEnvironmentCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "environment",
                "show",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Shows details of an environment

            Usage:
              nitro environment show <id> [options]

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
                "environment",
                "show",
                "environment-1")
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
    public async Task EnvironmentNotFound_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowEnvironmentAsync(
                "environment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowEnvironmentCommandQuery_Node?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "show",
                "environment-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The environment with ID 'environment-1' was not found.
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithEnvironmentId_ReturnSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowEnvironmentAsync(
                "environment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShowEnvironmentNode("environment-1", "production", "workspace-a"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "show",
                "environment-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "environment-1",
              "name": "production",
              "workspace": {
                "name": "workspace-a"
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
                "environment",
                "show",
                "environment-1")
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
                "environment",
                "show",
                "environment-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static IShowEnvironmentCommandQuery_Node CreateShowEnvironmentNode(
        string id,
        string name,
        string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace>(
            MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var node = new Mock<IShowEnvironmentCommandQuery_Node_Environment>(MockBehavior.Strict);
        node.SetupGet(x => x.Id).Returns(id);
        node.SetupGet(x => x.Name).Returns(name);
        node.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return node.Object;
    }

    private static Mock<IEnvironmentsClient> CreateShowExceptionClient(Exception ex)
    {
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowEnvironmentAsync(
                "environment-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
