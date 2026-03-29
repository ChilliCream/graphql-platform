using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class ListApiCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "api",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists all APIs of a workspace

            Usage:
              nitro api list [options]

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
                "api",
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
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage(
                null,
                false,
                ("api-1", "products", new[] { "products" }, "Workspace"),
                ("api-2", "catalog", new[] { "catalog" }, "Workspace")));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "list",
                "--workspace-id",
                "ws-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    │ api-2 │ catalog  │ catalog  │
                                    └───────┴──────────┴──────────┘
                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    │ api-2 │ catalog  │ catalog  │
                                    └───────┴──────────┴──────────┘
                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    │ api-2 │ catalog  │ catalog  │
                                    └───────┴──────────┴──────────┘                         {
              "id": "api-1",
              "name": "products",
              "path": "products",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage(
                null,
                false,
                ("api-1", "products", new[] { "products" }, "Workspace"),
                ("api-2", "catalog", new[] { "catalog" }, "Workspace")));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "list")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    │ api-2 │ catalog  │ catalog  │
                                    └───────┴──────────┴──────────┘
                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    │ api-2 │ catalog  │ catalog  │
                                    └───────┴──────────┴──────────┘
                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    │ api-2 │ catalog  │ catalog  │
                                    └───────┴──────────┴──────────┘                         {
              "id": "api-1",
              "name": "products",
              "path": "products",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage(
                "cursor-2",
                true,
                ("api-1", "products", new[] { "products" }, "Workspace"),
                ("api-2", "catalog", new[] { "catalog" }, "Workspace")));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "api-1",
                  "name": "products",
                  "path": "products",
                  "workspace": {
                    "name": "Workspace"
                  },
                  "apiDetailPromptSettings": {
                    "apiDetailPromptSchemaRegistry": {
                      "treatDangerousAsBreaking": true,
                      "allowBreakingSchemaChanges": false
                    }
                  }
                },
                {
                  "id": "api-2",
                  "name": "catalog",
                  "path": "catalog",
                  "workspace": {
                    "name": "Workspace"
                  },
                  "apiDetailPromptSettings": {
                    "apiDetailPromptSchemaRegistry": {
                      "treatDangerousAsBreaking": true,
                      "allowBreakingSchemaChanges": false
                    }
                  }
                }
              ],
              "cursor": "cursor-2"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage());

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "list",
                "--workspace-id",
                "ws-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                      APIs

            There was no data found.
                      APIs

            There was no data found.
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "list",
                "--workspace-id",
                "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "workspace-from-session",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage(
                null,
                false,
                ("api-1", "products", new[] { "products" }, "Workspace")));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "list",
                "--cursor",
                "cursor-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    └───────┴──────────┴──────────┘
                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    └───────┴──────────┴──────────┘
                                                  APIs

                                    ┌───────┬──────────┬──────────┐
                                    │ Id    │ Name     │ Path     │
                                    ├───────┼──────────┼──────────┤
                                    │ api-1 │ products │ products │
                                    └───────┴──────────┴──────────┘                         {
              "id": "api-1",
              "name": "products",
              "path": "products",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "workspace-from-session",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage(
                null,
                false,
                ("api-1", "products", new[] { "products" }, "Workspace")));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "list",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "api-1",
                  "name": "products",
                  "path": "products",
                  "workspace": {
                    "name": "Workspace"
                  },
                  "apiDetailPromptSettings": {
                    "apiDetailPromptSchemaRegistry": {
                      "treatDangerousAsBreaking": true,
                      "allowBreakingSchemaChanges": false
                    }
                  }
                }
              ],
              "cursor": null
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
        var client = CreateListExceptionClient(new NitroClientException("list failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments("api", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
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
        var client = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments("api", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static Mock<IApisClient> CreateListExceptionClient(Exception ex)
    {
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApisAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
