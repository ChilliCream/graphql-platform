using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class ListEnvironmentCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "environment",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists all environments of a workspace

            Usage:
              nitro environment list [options]

            Options:
              --cursor <cursor>              The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
              --workspace-id <workspace-id>  The ID of the workspace. [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>            The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information
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
                "list")
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
    public async Task NoWorkspaceInSession_And_NoWorkspaceOption_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListEnvironmentsPage(
                endCursor: null,
                hasNextPage: false,
                ("env-1", "production", "workspace-a"),
                ("env-2", "staging", "workspace-a")));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                              Environments

                                         ┌───────┬────────────┐
                                         │ Id    │ Name       │
                                         ├───────┼────────────┤
                                         │ env-1 │ production │
                                         │ env-2 │ staging    │
                                         └───────┴────────────┘
                                              Environments

                                         ┌───────┬────────────┐
                                         │ Id    │ Name       │
                                         ├───────┼────────────┤
                                         │ env-1 │ production │
                                         │ env-2 │ staging    │
                                         └───────┴────────────┘
                                              Environments

                                         ┌───────┬────────────┐
                                         │ Id    │ Name       │
                                         ├───────┼────────────┤
                                         │ env-1 │ production │
                                         │ env-2 │ staging    │
                                         └───────┴────────────┘
            {
              "id": "env-1",
              "name": "production",
              "workspace": {
                "name": "workspace-a"
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListEnvironmentsPage(
                endCursor: null,
                hasNextPage: false,
                ("env-1", "production", "workspace-a")));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "env-1",
                  "name": "production",
                  "workspace": {
                    "name": "workspace-a"
                  }
                }
              ],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListEnvironmentsPage(
                endCursor: null,
                hasNextPage: false,
                ("env-1", "production", "workspace-a")));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "env-1",
                  "name": "production",
                  "workspace": {
                    "name": "workspace-a"
                  }
                }
              ],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListEnvironmentsPage(
                endCursor: null,
                hasNextPage: false,
                ("env-2", "staging", "workspace-a")));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1",
                "--cursor",
                "cursor-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                              Environments

                                          ┌───────┬─────────┐
                                          │ Id    │ Name    │
                                          ├───────┼─────────┤
                                          │ env-2 │ staging │
                                          └───────┴─────────┘
                                              Environments

                                          ┌───────┬─────────┐
                                          │ Id    │ Name    │
                                          ├───────┼─────────┤
                                          │ env-2 │ staging │
                                          └───────┴─────────┘
                                              Environments

                                          ┌───────┬─────────┐
                                          │ Id    │ Name    │
                                          ├───────┼─────────┤
                                          │ env-2 │ staging │
                                          └───────┴─────────┘
            {
              "id": "env-2",
              "name": "staging",
              "workspace": {
                "name": "workspace-a"
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListEnvironmentsPage(
                endCursor: null,
                hasNextPage: false,
                ("env-2", "staging", "workspace-a")));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "env-2",
                  "name": "staging",
                  "workspace": {
                    "name": "workspace-a"
                  }
                }
              ],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                "ws-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListEnvironmentsPage(
                endCursor: null,
                hasNextPage: false,
                ("env-2", "staging", "workspace-a")));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "env-2",
                  "name": "staging",
                  "workspace": {
                    "name": "workspace-a"
                  }
                }
              ],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientException("list failed"), "ws-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientException("list failed"), "ws-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientException("list failed"), "ws-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"), "ws-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"), "ws-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"), "ws-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "environment",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>
        CreateListEnvironmentsPage(
            string? endCursor = null,
            bool hasNextPage = false,
            params (string Id, string Name, string WorkspaceName)[] environments)
    {
        var items = environments
            .Select(static environment =>
                CreateEnvironmentNode(environment.Id, environment.Name, environment.WorkspaceName))
            .ToArray();

        return new ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>(
            items,
            endCursor,
            hasNextPage);
    }

    private static IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node CreateEnvironmentNode(
        string id,
        string name,
        string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace>(
            MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var node = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>(MockBehavior.Strict);
        node.SetupGet(x => x.Id).Returns(id);
        node.SetupGet(x => x.Name).Returns(name);
        node.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return node.Object;
    }

    private static Mock<IEnvironmentsClient> CreateListExceptionClient(
        Exception ex,
        string workspaceId,
        string? cursor)
    {
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListEnvironmentsAsync(
                workspaceId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
