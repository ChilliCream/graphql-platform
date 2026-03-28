using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

// TODO: Add tests for mutation errors being returned
public sealed class CreateApiKeyCommandTests
{
    [Fact]
    public async Task Help_ReturnsResult()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddArguments(
                "api-key",
                "create",
                "--help");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            Description:
              Creates a new API key

            Usage:
              nitro api-key create [options]

            Options:
              --name <name>                        The name of the API key (for later reference) [env: NITRO_API_KEY_NAME]
              --api-id <api-id>                    The ID of the API [env: NITRO_API_ID]
              --workspace-id <workspace-id>        The ID of the workspace. [env: NITRO_WORKSPACE_ID]
              --stage-condition <stage-condition>  **PREVIEW** Limit the API key to a specific stage name. If not provided, the API key will be valid for all stages.
              --cloud-url <cloud-url>              The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                  The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                      The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                       Show help and usage information
            """);
        client.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "key-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--name'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_SelectsApi_ReturnsResult()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var command = await new CommandBuilder()
            .AddService(client.Object)
            .AddSession("workspace-from-session")
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "create")
            .StartAsync();

        // act
        await command.InputAsync("integration"); // name
        await command.SelectOptionAsync("Api"); // Api or Workspace
        await command.SelectOptionAsync("api-1"); // TODO: This probably needs to change

        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_SelectsWorkspace_ReturnsResult()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var command = await new CommandBuilder()
            .AddService(client.Object)
            .AddSession("workspace-from-session")
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "create")
            .StartAsync();

        // act
        await command.InputAsync("integration"); // name
        await command.SelectOptionAsync("Workspace"); // Api or Workspace

        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingWorkspaceAndApi_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "key-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            The '--workspace-id' or '--api-id' option is required in non-interactive mode.
            """);
        client.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_NoWorkspaceIdOption_NoWorkspaceInSession_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "key-1",
                "--api-id",
                "api-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            The '--workspace-id' or '--api-id' option is required in non-interactive mode.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsResult_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "ws-1",
                null,
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "integration",
                "--stage-condition",
                "prod");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "integration",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsResult_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "ws-1",
                null,
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "integration",
                "--stage-condition",
                "prod");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "integration",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceIdFromSession_ReturnsResult_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddSession("workspace-from-session")
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceId_ReturnsResult_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "ws-1",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddSession()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--workspace-id",
                "ws-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceId_ReturnsResult_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "ws-1",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddSession()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--workspace-id",
                "ws-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceIdFromSession_ReturnsResult_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddSession("workspace-from-session")
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("create failed"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddSession("workspace-from-session")
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            create failed
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("create failed"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddSession("workspace-from-session")
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            create failed
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("create failed"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddSession("workspace-from-session")
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            create failed
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException("create failed"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddSession("workspace-from-session")
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            create failed
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException("create failed"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddSession("workspace-from-session")
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            create failed
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException("create failed"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddSession("workspace-from-session")
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            create failed
            """);
        client.VerifyAll();
    }
}
