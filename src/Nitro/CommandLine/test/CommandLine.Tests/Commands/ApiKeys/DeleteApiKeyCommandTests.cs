using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class DeleteApiKeyCommandTests(NitroCommandFixture fixture) : ApiKeysCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api-key",
            "delete",
            "--help");

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

            Example:
              nitro api-key delete "<api-key-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "delete",
            ApiKeyId,
            "--force");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task DeleteApiKeyThrows_ReturnsError()
    {
        // arrange
        SetupDeleteApiKeyMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "delete",
            ApiKeyId,
            "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API key 'key-1'
            └── ✕ Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetDeleteApiKeyErrors))]
    public async Task DeleteApiKeyHasErrors_ReturnsError(
        IDeleteApiKeyCommandMutation_DeleteApiKey_Errors error,
        string expectedStdErr)
    {
        // arrange
        SetupDeleteApiKeyMutation(ApiKeyId, error);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "delete",
            ApiKeyId,
            "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API key 'key-1'
            └── ✕ Failed to delete the API key.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithoutForce_PromptsUser_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupDeleteApiKeyMutation();

        var command = StartInteractiveCommand(
            "api-key",
            "delete",
            ApiKeyId);

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithoutForce_PromptsUser_Declined_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "api-key",
            "delete",
            ApiKeyId);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            API key was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess()
    {
        // arrange
        SetupDeleteApiKeyMutation();

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "delete",
            ApiKeyId,
            "--force");

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
    }

    public static TheoryData<IDeleteApiKeyCommandMutation_DeleteApiKey_Errors, string>
        GetDeleteApiKeyErrors()
    {
        var apiKeyNotFound =
            new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey_Errors_ApiKeyNotFoundError>(MockBehavior.Strict);
        apiKeyNotFound.SetupGet(x => x.ApiKeyId).Returns("key-1");
        apiKeyNotFound.As<IApiKeyNotFoundError>().SetupGet(x => x.Message).Returns("API key not found");
        apiKeyNotFound.As<IError>().SetupGet(x => x.Message).Returns("API key not found");

        var unknownError =
            new Mock<IDeleteApiKeyCommandMutation_DeleteApiKey_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        unknownError.As<IError>().SetupGet(x => x.Message).Returns("Unauthorized");

        return new()
        {
            { apiKeyNotFound.Object, "API key not found" },
            { unknownError.Object, "Unexpected mutation error: Unauthorized" }
        };
    }
}
