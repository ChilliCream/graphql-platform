using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class DeleteApiKeyCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "api-key",
                "delete",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete an API key by ID.

            Usage:
              nitro api-key delete <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
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
                "api-key",
                "delete",
                "key-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task WithApiKeyAndForce_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateDeleteApiKeyResult("key-1", "my-key", "Workspace"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Deleting API key 'key-1'
            └── ✓ Deleted API key 'key-1'.

            {
              "id": "key-1",
              "name": "my-key",
              "workspace": {
                "name": "Workspace"
              }
            }
            """);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteApiKeyMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IDeleteApiKeyCommandMutation_DeleteApiKey_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateDeleteApiKeyResultWithErrors(mutationError));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteApiKeyMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IDeleteApiKeyCommandMutation_DeleteApiKey_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateDeleteApiKeyResultWithErrors(mutationError));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API key 'key-1'
            └── ✕ Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteApiKeyMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IDeleteApiKeyCommandMutation_DeleteApiKey_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateDeleteApiKeyResultWithErrors(mutationError));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

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
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API key 'key-1'
            └── ✕ Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API key 'key-1'
            └── ✕ Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync(
                "key-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var builder = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "delete",
                "key-1",
                "--force");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    public static IEnumerable<object[]> DeleteApiKeyMutationErrorCases()
    {
        var apiKeyNotFound =
            new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey_Errors_ApiKeyNotFoundError>(MockBehavior.Strict);
        apiKeyNotFound.SetupGet(x => x.ApiKeyId).Returns("key-1");
        apiKeyNotFound.As<IApiKeyNotFoundError>().SetupGet(x => x.Message).Returns("API key not found");
        apiKeyNotFound.As<IError>().SetupGet(x => x.Message).Returns("API key not found");

        var unknownError =
            new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        unknownError.As<IError>().SetupGet(x => x.Message).Returns("Unauthorized");

        return new[]
        {
            new object[] { apiKeyNotFound.Object, "API key not found" },
            new object[] { unknownError.Object, "Unexpected mutation error: Unauthorized" }
        };
    }
}
