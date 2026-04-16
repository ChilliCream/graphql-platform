using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class EditStagesCommandTests(NitroCommandFixture fixture) : StagesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Edit stages of an API.

            Usage:
              nitro stage edit [options]

            Options:
              --api-id <api-id>                The ID of the API [env: NITRO_API_ID]
              --configuration <configuration>  The stage configuration. If not provided, an interactive selection will beshown. This input is a JSON array of stage configuration in the following format:[{"name":"stage1","displayName":"Stage 1","conditions":[{"afterStage":"stage2"}]},...]
              --cloud-url <cloud-url>          The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>              The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                   Show help and usage information

            Example:
              nitro stage edit \
                --configuration "[{\"name\":\"dev\",\"displayName\":\"Dev\",\"conditions\":[]}]" \
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
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task MissingApiId_ReturnsError()
    {
        // arrange
        SetupSession();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.AssertError(
            """
            Missing required option '--api-id'.
            """);
    }

    [Fact]
    public async Task WithJsonConfig_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupUpdateStagesMutation();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithJsonConfig_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupUpdateStagesMutation();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.AssertSuccess(
            """
            Updating stages for API 'api-1'
            └── ✓ Updated stages for API 'api-1'.

            {
              "values": [
                {
                  "id": "stage-1",
                  "name": "dev",
                  "conditions": []
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithJsonConfig_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupUpdateStagesMutation();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "stage-1",
                  "name": "dev",
                  "conditions": []
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithJsonConfig_WithConditions_ReturnsSuccess()
    {
        // arrange
        SetupUpdateStagesMutation();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]},{"name":"prod","displayName":"Production","conditions":[{"afterStage":"dev"}]}]""");

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithInvalidJsonConfig_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            "not-valid-json");

        // assert
        result.AssertError(
            """
            Could not parse stage configuration
            """);
    }

    [Fact]
    public async Task UpdateStagesThrows_ReturnsError()
    {
        // arrange
        SetupUpdateStagesMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating stages for API 'api-1'
            └── ✕ Failed to update the stages.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUpdateStagesErrors))]
    public async Task UpdateStagesHasErrors_ReturnsError(
        IUpdateStages_UpdateStages_Errors error,
        string expectedStdOut)
    {
        // arrange
        SetupUpdateStagesMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.StdOut.MatchInlineSnapshot(expectedStdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            Stage update failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UpdateStagesReturnsNullApi_ReturnsSuccess()
    {
        // arrange
        SetupUpdateStagesMutationNullApi();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "edit",
            "--api-id",
            ApiId,
            "--configuration",
            """[{"name":"dev","displayName":"Dev","conditions":[]}]""");

        // assert
        result.AssertSuccess();
    }

    public static TheoryData<IUpdateStages_UpdateStages_Errors, string>
        GetUpdateStagesErrors() => new()
    {
        {
            CreateUpdateStagesApiNotFoundError(),
            """
            Updating stages for API 'api-1'
            └── ✕ Failed to update the stages.
                └── API not found
            """
        },
        {
            CreateUpdateStagesStageNotFoundError(),
            """
            Updating stages for API 'api-1'
            └── ✕ Failed to update the stages.
                └── Stage not found
            """
        },
        {
            CreateUpdateStagesStagesHavePublishedDependenciesError(),
            """
            Updating stages for API 'api-1'
            └── ✕ Failed to update the stages.
                └── Stages have published dependencies
            """
        },
        {
            CreateUpdateStagesStageValidationError(),
            """
            Updating stages for API 'api-1'
            └── ✕ Failed to update the stages.
                └── Stage validation failed
            """
        }
    };
}
