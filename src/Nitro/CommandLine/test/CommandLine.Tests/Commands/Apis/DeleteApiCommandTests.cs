using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class DeleteApiCommandTests(NitroCommandFixture fixture) : ApisCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api",
            "delete",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete an API by ID.

            Usage:
              nitro api delete <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro api delete "<api-id>"
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
            "api",
            "delete",
            ApiId,
            "--force");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task ApiNotFound_ReturnsError()
    {
        // arrange
        SetupGetApiForDeleteQueryNull(ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "delete",
            ApiId,
            "--force");

        // assert
        result.AssertError(
            """
            The API with ID 'api-1' was not found.
            """);
    }

    [Fact]
    public async Task DeleteApiThrows_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetApiForDeleteQuery(ApiId, ApiName);
        SetupDeleteApiMutationException(ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "delete",
            ApiId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetDeleteApiErrors))]
    public async Task DeleteApiHasErrors_ReturnsError(
        IDeleteApiCommandMutation_DeleteApiById_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetApiForDeleteQuery(ApiId, ApiName);
        SetupDeleteApiMutation(ApiId, ApiName, ["products"], error);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "delete",
            ApiId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task DeleteApiReturnsNullApi_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetApiForDeleteQuery(ApiId, ApiName);
        SetupDeleteApiMutationNullApi(ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "delete",
            ApiId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NonInteractiveWithoutForce_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(mode);
        SetupGetApiForDeleteQuery(ApiId, ApiName);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "delete",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Attempted to prompt the user for confirmation, but the console is running in non-interactive mode.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupGetApiForDeleteQuery(ApiId, ApiName);

        var command = StartInteractiveCommand(
            "api",
            "delete",
            ApiId);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The API was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task PromptAndConfirm_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupGetApiForDeleteQuery(ApiId, ApiName);
        SetupDeleteApiMutation(ApiId, ApiName, ["products"]);

        var command = StartInteractiveCommand(
            "api",
            "delete",
            ApiId);

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess()
    {
        // arrange
        SetupGetApiForDeleteQuery(ApiId, ApiName);
        SetupDeleteApiMutation(ApiId, ApiName, ["products"]);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "delete",
            ApiId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Deleting API 'api-1'
            └── ✓ Deleted API 'api-1'.

            {
              "id": "api-1",
              "name": "my-api",
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
    }

    public static TheoryData<IDeleteApiCommandMutation_DeleteApiById_Errors, string>
        GetDeleteApiErrors() => new()
    {
        {
            new DeleteApiCommandMutation_DeleteApiById_Errors_ApiNotFoundError("API not found"),
            "Unexpected mutation error: API not found"
        },
        {
            new DeleteApiCommandMutation_DeleteApiById_Errors_UnauthorizedOperation("Not authorized"),
            "Unexpected mutation error: Not authorized"
        },
        {
            new DeleteApiCommandMutation_DeleteApiById_Errors_ApiDeletionFailedError("Deletion failed"),
            "Unexpected mutation error: Deletion failed"
        }
    };
}
