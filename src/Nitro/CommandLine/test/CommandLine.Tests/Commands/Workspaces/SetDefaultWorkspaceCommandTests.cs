using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class SetDefaultWorkspaceCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "workspace",
                "set-default",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Select a workspace and set it as the default workspace.

            Usage:
              nitro workspace set-default [options]

            Options:
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
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaces_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
                [], null, false));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You do not have any workspaces. Run `[bold blue]nitro launch[/]` and create one.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task SelectsWorkspace_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePage(
                new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
                    "ws-1", "my-workspace", false)));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "set-default")
            .Start();

        // act
        command.SelectOption(0);

        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        result.StdOut.MatchInlineSnapshot(
            """
            ? Which workspace do you want to use as your default?

            > my-workspace                                       ? Which workspace do you want to use as your default?: my-workspace
            """);

        client.VerifyAll();
    }

    private static Mock<IWorkspacesClient> CreateSelectExceptionClient(Exception ex)
    {
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>
        CreateWorkspacePage(
            params ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node[] items)
    {
        return new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
            items, null, false);
    }
}
