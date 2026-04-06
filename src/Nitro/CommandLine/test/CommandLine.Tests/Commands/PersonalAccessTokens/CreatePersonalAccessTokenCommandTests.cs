using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class CreatePersonalAccessTokenCommandTests(NitroCommandFixture fixture)
    : PersonalAccessTokensCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "pat",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new personal access token.

            Usage:
              nitro pat create [options]

            Options:
              --description <description>  The description of the personal access token [env: NITRO_DESCRIPTION]
              --expires <expires>          The expiration time of the personal access token in days [env: NITRO_EXPIRES] [default: 180]
              --cloud-url <cloud-url>      The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>          The API key used for authentication [env: NITRO_API_KEY]
              --output <json>              The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help               Show help and usage information

            Example:
              nitro pat create \
                --description "CI/CD token" \
                --expires "30"
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
            "create",
            "--description",
            PatDescription);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredDescription_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "create");

        // assert
        result.AssertError(
            """
            Missing required option '--description'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupCreatePersonalAccessTokenMutation();

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "create",
            "--description",
            PatDescription);

        // assert
        result.AssertSuccess(
            """
            Creating personal access token
            └── ✓ Created personal access token 'my-token'.

            {
              "secret": "secret-123",
              "details": {
                "id": "pat-1",
                "description": "my-token",
                "createdAt": "2025-01-01T00:00:00+00:00",
                "expiresAt": "2025-06-01T00:00:00+00:00"
              }
            }
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupCreatePersonalAccessTokenMutation();

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "create",
            "--description",
            PatDescription);

        // assert
        result.AssertSuccess(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "pat-1",
                "description": "my-token",
                "createdAt": "2025-01-01T00:00:00+00:00",
                "expiresAt": "2025-06-01T00:00:00+00:00"
              }
            }
            """);
    }

    [Fact]
    public async Task CreatePersonalAccessTokenReturnsNullResult_ReturnsError()
    {
        // arrange
        SetupCreatePersonalAccessTokenMutationNullResult();

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "create",
            "--description",
            PatDescription);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating personal access token
            └── ✕ Failed to create the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetCreatePersonalAccessTokenErrors))]
    public async Task CreatePersonalAccessTokenHasErrors_ReturnsError(
        ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupCreatePersonalAccessTokenMutation(mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "create",
            "--description",
            PatDescription);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating personal access token
            └── ✕ Failed to create the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreatePersonalAccessTokenThrows_ReturnsError()
    {
        // arrange
        SetupCreatePersonalAccessTokenMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "create",
            "--description",
            PatDescription);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating personal access token
            └── ✕ Failed to create the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors, string>
        GetCreatePersonalAccessTokenErrors() => new()
    {
        { CreateCreatePersonalAccessTokenUnauthorizedError(), "Not authorized" },
        { CreateCreatePersonalAccessTokenValidationError(), "Unexpected mutation error: Validation failed" }
    };
}
