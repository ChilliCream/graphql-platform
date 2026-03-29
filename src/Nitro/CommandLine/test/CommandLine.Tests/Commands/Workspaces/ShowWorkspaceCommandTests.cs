using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Workspaces;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class ShowWorkspaceCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "workspace",
                "show",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Shows details of a workspace

            Usage:
              nitro workspace show <id> [options]

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
                "workspace",
                "show",
                "ws-1")
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
    public async Task WorkspaceNotFound_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowWorkspaceAsync(
                "ws-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowWorkspaceCommandQuery_Node?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "show",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The workspace with ID 'ws-1' was not found.
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task WithWorkspaceId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowWorkspaceAsync(
                "ws-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShowWorkspaceCommandQuery_Node_Workspace("ws-1", "my-workspace", false));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "show",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowWorkspaceAsync(
                "ws-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShowWorkspaceCommandQuery_Node_Workspace("ws-1", "my-workspace", false));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "show",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
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
                "workspace",
                "show",
                "ws-1")
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
                "workspace",
                "show",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static Mock<IWorkspacesClient> CreateShowExceptionClient(Exception ex)
    {
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowWorkspaceAsync(
                "ws-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
