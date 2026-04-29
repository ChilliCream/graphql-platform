using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class DeleteStageCommandTests(NitroCommandFixture fixture) : StagesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "stage",
            "delete",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete a stage by name.

            Usage:
              nitro stage delete [options]

            Options:
              --api-id <api-id>           The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)  The name of the stage [env: NITRO_STAGE]
              --force                     Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>         The API key used for authentication [env: NITRO_API_KEY]
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information

            Example:
              nitro stage delete \
                --stage "dev" \
                --api-id "<api-id>"
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
            "stage",
            "delete",
            "--api-id",
            ApiId,
            "--stage",
            StageName,
            "--force");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "stage",
            "delete",
            "--api-id",
            ApiId,
            "--stage",
            StageName);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Stage was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithConfirmation_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupForceDeleteStageMutation();

        var command = StartInteractiveCommand(
            "stage",
            "delete",
            "--api-id",
            ApiId,
            "--stage",
            StageName);

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task ForceDeleteStageThrows_ReturnsError()
    {
        // arrange
        SetupForceDeleteStageMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "delete",
            "--api-id",
            ApiId,
            "--stage",
            StageName,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetForceDeleteStageErrors))]
    public async Task ForceDeleteStageHasErrors_ReturnsError(
        IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors error,
        string expectedStdErr)
    {
        // arrange
        SetupForceDeleteStageMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "delete",
            "--api-id",
            ApiId,
            "--stage",
            StageName,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ForceDeleteStageReturnsNullApi_ReturnsError()
    {
        // arrange
        SetupForceDeleteStageMutationNullApi();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "delete",
            "--api-id",
            ApiId,
            "--stage",
            StageName,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors, string>
        GetForceDeleteStageErrors() => new()
    {
        { CreateForceDeleteStageApiNotFoundError(), "API not found" },
        { CreateForceDeleteStageStageNotFoundError(), "Stage not found" },
        { CreateForceDeleteStageUnauthorizedError(), "Not authorized" }
    };
}
