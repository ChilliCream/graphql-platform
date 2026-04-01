using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class ListApiKeyCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "api-key",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all API keys of a workspace.

            Usage:
              nitro api-key list [options]

            Options:
                            --cursor <cursor>              The pagination cursor to resume from [env: NITRO_CURSOR]
                            --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
                            --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>            The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
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
        var result = await new CommandBuilder(fixture)
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
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess_Interactive()
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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_Interactive()
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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess(InteractionMode mode)
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

        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
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

        var result = await new CommandBuilder(fixture)
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

        var result = await new CommandBuilder(fixture)
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
    public async Task WithCursor_ReturnsSuccess_Interactive()
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
                    endCursor: null,
                    hasNextPage: false,
                    ("key-1", "tenant-key", "Workspace"),
                    ("key-2", "integration-key", "Workspace")));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
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
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
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
                    endCursor: null,
                    hasNextPage: false,
                    ("key-1", "tenant-key", "Workspace"),
                    ("key-2", "integration-key", "Workspace")));

        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(mode)
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
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(mode)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
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
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.ListApiKeysAsync(
                "workspace-from-session",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(mode)
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
