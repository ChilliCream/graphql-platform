using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class SetApiSettingsCommandTests(NitroCommandFixture fixture) : ApisCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api",
            "set-settings",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Set the settings of an API.

            Usage:
              nitro api set-settings <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
                            --treat-dangerous-as-breaking    Treat dangerous changes as breaking [env: NITRO_TREAT_DANGEROUS_AS_BREAKING]
                            --allow-breaking-schema-changes  Allow breaking schema changes when no client breaks [env: NITRO_ALLOW_BREAKING_SCHEMA_CHANGES]
                            --cloud-url <cloud-url>          The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>              The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>                  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help                   Show help and usage information

            Example:
              nitro api set-settings "<api-id>" \
                --treat-dangerous-as-breaking \
                --allow-breaking-schema-changes
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
            "set-settings",
            ApiId,
            "--treat-dangerous-as-breaking",
            "true",
            "--allow-breaking-schema-changes",
            "false");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess()
    {
        // arrange
        SetupUpdateApiSettingsMutation(ApiId, true, false);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "set-settings",
            ApiId,
            "--treat-dangerous-as-breaking",
            "true",
            "--allow-breaking-schema-changes",
            "false");

        // assert
        result.AssertSuccess(
            """
            Updating settings for API 'api-1'
            └── ✓ Updated settings for API 'api-1'.

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

    [Fact]
    public async Task UpdateApiSettingsThrows_ReturnsError()
    {
        // arrange
        SetupUpdateApiSettingsMutationException(ApiId);
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "set-settings",
            ApiId,
            "--treat-dangerous-as-breaking",
            "true",
            "--allow-breaking-schema-changes",
            "false");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Updating settings for API 'api-1'
            └── ✕ Failed to update the API settings.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUpdateApiSettingsErrors))]
    public async Task UpdateApiSettingsHasErrors_ReturnsError(
        ISetApiSettingsCommandMutation_UpdateApiSettings_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupUpdateApiSettingsMutation(ApiId, true, false, errors: error);
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "set-settings",
            ApiId,
            "--treat-dangerous-as-breaking",
            "true",
            "--allow-breaking-schema-changes",
            "false");

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Updating settings for API 'api-1'
            └── ✕ Failed to update the API settings.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNoData_ReturnsError_NonInteractive()
    {
        // arrange
        SetupUpdateApiSettingsMutationNullResult(ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "set-settings",
            ApiId,
            "--treat-dangerous-as-breaking",
            "true",
            "--allow-breaking-schema-changes",
            "false");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating settings for API 'api-1'
            └── ✕ Failed to update the API settings.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task PromptsForTreatDangerousAsBreaking_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupUpdateApiSettingsMutation(ApiId, true, false);
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "api",
            "set-settings",
            ApiId,
            "--allow-breaking-schema-changes",
            "false");

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task PromptsForAllowBreakingChanges_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupUpdateApiSettingsMutation(ApiId, true, false);
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "api",
            "set-settings",
            ApiId,
            "--treat-dangerous-as-breaking",
            "true");

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingOptions_ReturnsError_NonInteractive(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "set-settings",
            ApiId);

        // assert
        result.AssertError(
            """
            Missing required option '--treat-dangerous-as-breaking'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingId_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "set-settings");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Required argument missing for command: 'set-settings'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<ISetApiSettingsCommandMutation_UpdateApiSettings_Errors, string>
        GetUpdateApiSettingsErrors() => new()
    {
        { CreateUpdateApiSettingsApiNotFoundError(), "API not found" },
        { CreateUpdateApiSettingsUnauthorizedError(), "Not authorized" },
        { CreateUpdateApiSettingsUnknownError(), "Unexpected mutation error: payload denied" }
    };
}
