using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class ListApiKeyCommandTests
{
    [Fact]
    public async Task Help_ReturnsResult()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "api-key",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Description:
              Lists all API keys of a workspace

            Usage:
              testhost api-key list [options]

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
    public async Task NoSession_And_NoWorkspaceId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task WithWorkspaceIdFromSession_ReturnsResult_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateListApiKeysPage(
                    apiKeys:
                    [
                        ("key-1", "tenant-key", "Workspace"),
                        ("key-2", "integration-key", "Workspace")
                    ]));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "list")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess(
            """

                                                                        API Keys

                                                    ┌───────┬─────────────────┐
                                                    │ Id    │ Name            │
                                                    ├───────┼─────────────────┤
                                                    │ key-1 │ tenant-key      │
                                                    │ key-2 │ integration-key │
                                                    └───────┴─────────────────┘
                                                                        API Keys

                                                    ┌───────┬─────────────────┐
                                                    │ Id    │ Name            │
                                                    ├───────┼─────────────────┤
                                                    │ key-1 │ tenant-key      │
                                                    │ key-2 │ integration-key │
                                                    └───────┴─────────────────┘
                                                                        API Keys

                                                    ┌───────┬─────────────────┐
                                                    │ Id    │ Name            │
                                                    ├───────┼─────────────────┤
                                                    │ key-1 │ tenant-key      │
                                                    │ key-2 │ integration-key │
                                                    └───────┴─────────────────┘                           {
              "id": "key-1",
              "name": "tenant-key",
              "workspace": {
                "name": "Workspace"
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsResult_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateListApiKeysPage(
                    apiKeys:
                    [
                        ("key-1", "tenant-key", "Workspace"),
                        ("key-2", "integration-key", "Workspace")
                    ]));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "list",
                "--workspace-id",
                "ws-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess(
            """

                                                                        API Keys

                                                    ┌───────┬─────────────────┐
                                                    │ Id    │ Name            │
                                                    ├───────┼─────────────────┤
                                                    │ key-1 │ tenant-key      │
                                                    │ key-2 │ integration-key │
                                                    └───────┴─────────────────┘
                                                                        API Keys

                                                    ┌───────┬─────────────────┐
                                                    │ Id    │ Name            │
                                                    ├───────┼─────────────────┤
                                                    │ key-1 │ tenant-key      │
                                                    │ key-2 │ integration-key │
                                                    └───────┴─────────────────┘
                                                                        API Keys

                                                    ┌───────┬─────────────────┐
                                                    │ Id    │ Name            │
                                                    ├───────┼─────────────────┤
                                                    │ key-1 │ tenant-key      │
                                                    │ key-2 │ integration-key │
                                                    └───────┴─────────────────┘                           {
              "id": "key-1",
              "name": "tenant-key",
              "workspace": {
                "name": "Workspace"
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateListApiKeysPage());

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "list",
                "--workspace-id",
                "ws-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess(
            """

                                                            API Keys

                There was no data found.
                    API Keys

                There was no data found.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceIdFromSession_ReturnsResult_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateListApiKeysPage(
                    endCursor: "cursor-2",
                    hasNextPage: true,
                    ("key-1", "tenant-key", "Workspace"),
                    ("key-2", "integration-key", "Workspace")));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
                "values": [
                    {
                        "id": "key-1",
                        "name": "tenant-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    },
                    {
                        "id": "key-2",
                        "name": "integration-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    }
                ],
                "cursor": "cursor-2"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceIdFromSession_ReturnsResult_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateListApiKeysPage(
                    endCursor: "cursor-2",
                    hasNextPage: true,
                    ("key-1", "tenant-key", "Workspace"),
                    ("key-2", "integration-key", "Workspace")));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "list")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            {
                "values": [
                    {
                        "id": "key-1",
                        "name": "tenant-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    },
                    {
                        "id": "key-2",
                        "name": "integration-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    }
                ],
                "cursor": "cursor-2"
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateListApiKeysPage());

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
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
    public async Task WithWorkspaceId_NoData_ReturnsSuccess_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateListApiKeysPage());

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
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
    public async Task WithCursor_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateListApiKeysPage(
                    endCursor: "cursor-2",
                    hasNextPage: true,
                    ("key-1", "tenant-key", "Workspace"),
                    ("key-2", "integration-key", "Workspace")));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
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
                        "id": "key-1",
                        "name": "tenant-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    },
                    {
                        "id": "key-2",
                        "name": "integration-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    }
                ],
                "cursor": "cursor-2"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "ws-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateListApiKeysPage(
                    endCursor: "cursor-2",
                    hasNextPage: true,
                    ("key-1", "tenant-key", "Workspace"),
                    ("key-2", "integration-key", "Workspace")));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
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
                        "id": "key-1",
                        "name": "tenant-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    },
                    {
                        "id": "key-2",
                        "name": "integration-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    }
                ],
                "cursor": "cursor-2"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("list failed"));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "list")
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
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("list failed"));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("list failed"));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "list")
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
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException("list failed"));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "list")
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
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException("list failed"));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException("list failed"));

        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }
}
