using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class RevokePersonalAccessTokenCommandTests(NitroCommandFixture fixture)
    : PersonalAccessTokensCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "pat",
            "revoke",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Revoke a personal access token.

            Usage:
              nitro pat revoke <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro pat revoke "<pat-id>"
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
            "pat",
            "revoke",
            "pat-1");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task ConfirmationRejected_ReturnsError_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "pat",
            "revoke",
            "pat-1");

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            PAT was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithConfirmation_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupRevokePersonalAccessTokenMutation();

        var command = StartInteractiveCommand(
            "pat",
            "revoke",
            "pat-1");

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupRevokePersonalAccessTokenMutation();

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "revoke",
            "pat-1",
            "--force");

        // assert
        result.AssertSuccess(
            """
            Revoking personal access token 'pat-1'
            └── ✓ Revoked personal access token 'pat-1'.

            {
              "id": "pat-1",
              "description": "my-token",
              "createdAt": "2025-01-01T00:00:00+00:00",
              "expiresAt": "2025-06-01T00:00:00+00:00"
            }
            """);
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupRevokePersonalAccessTokenMutation();

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "revoke",
            "pat-1",
            "--force");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "pat-1",
              "description": "my-token",
              "createdAt": "2025-01-01T00:00:00+00:00",
              "expiresAt": "2025-06-01T00:00:00+00:00"
            }
            """);
    }

    [Fact]
    public async Task RevokePersonalAccessTokenReturnsNullPersonalAccessToken_ReturnsError()
    {
        // arrange
        SetupRevokePersonalAccessTokenMutationNullPersonalAccessToken();

        // act
        var result = await ExecuteCommandAsync(
            "pat", "revoke", PatId, "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Revoking personal access token 'pat-1'
            └── ✕ Failed to revoke the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not revoke personal access token.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetRevokePersonalAccessTokenErrors))]
    public async Task RevokePersonalAccessTokenHasErrors_ReturnsError(
        IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors error,
        string expectedStdErr)
    {
        // arrange
        SetupRevokePersonalAccessTokenMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "pat", "revoke", PatId, "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Revoking personal access token 'pat-1'
            └── ✕ Failed to revoke the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task RevokePersonalAccessTokenThrows_ReturnsError()
    {
        // arrange
        SetupRevokePersonalAccessTokenMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "pat", "revoke", PatId, "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Revoking personal access token 'pat-1'
            └── ✕ Failed to revoke the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors, string>
        GetRevokePersonalAccessTokenErrors() => new()
    {
        { CreateRevokePersonalAccessTokenNotFoundError(), "PAT not found" },
        { CreateRevokePersonalAccessTokenUnauthorizedError(), "Unexpected mutation error: Not authorized" }
    };
}
