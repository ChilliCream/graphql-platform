using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class ValidateMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : McpCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "validate",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate an MCP feature collection version.

            Usage:
              nitro mcp validate [options]

            Options:
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              --stage <stage> (REQUIRED)                                          The name of the stage [env: NITRO_STAGE]
              -p, --prompt-pattern <prompt-pattern>                               One or more file patterns to locate MCP prompt definition files (*.json)
              -t, --tool-pattern <tool-pattern>                                   One or more file patterns to locate MCP tool definition files (*.graphql)
              --cloud-url <cloud-url>                                             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                      Show help and usage information

            Example:
              nitro mcp validate \
                --mcp-feature-collection-id "<collection-id>" \
                --stage "dev" \
                --prompt-pattern "./prompts/**/*.json" \
                --tool-pattern "./tools/**/*.graphql"
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
            "mcp",
            "validate",
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task StartMcpFeatureCollectionValidationThrows_ReturnsError()
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupValidateMcpFeatureCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "validate",
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection 'mcp-1' against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetStartMcpFeatureCollectionValidationErrors))]
    public async Task StartMcpFeatureCollectionValidationHasErrors_ReturnsError(
        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupValidateMcpFeatureCollectionMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "validate",
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection 'mcp-1' against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task StartMcpFeatureCollectionValidationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupValidateMcpFeatureCollectionMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "validate",
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection 'mcp-1' against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ReturnsSuccess()
    {
        // arrange
        SetupMcpDefinitionFiles();
        var capturedStream = SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "validate",
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        await AssertMcpFeatureCollectionArchive(capturedStream);
        result.AssertSuccess(
            """
            Validating MCP feature collection 'mcp-1' against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   └── ✓ Validation passed.
            └── ✓ Validated MCP feature collection against stage 'dev'.
            """);
    }

    [Fact]
    public async Task WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupEnvironmentVariable(EnvironmentVariables.McpFeatureCollectionId, McpFeatureCollectionId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);

        var capturedStream = SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "validate",
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        await AssertMcpFeatureCollectionArchive(capturedStream);
        result.AssertSuccess(
            """
            Validating MCP feature collection 'mcp-1' against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   └── ✓ Validation passed.
            └── ✓ Validated MCP feature collection against stage 'dev'.
            """);
    }

    [Fact]
    public async Task BreakingChanges_ReturnsError()
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionValidationFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "validate",
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection 'mcp-1' against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   └── ✕ Validation failed.
            │       └── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │           └── Tool 'Fail'
            │               └── Invalid tool definition. (1:14)
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors, string>
        GetStartMcpFeatureCollectionValidationErrors()
    {
        var unexpectedError = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        return new()
        {
            {
                new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to validate."),
                "Not authorized to validate."
            },
            {
                new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    Stage),
                "Stage not found."
            },
            {
                new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                    McpFeatureCollectionId,
                    "MCP Feature Collection not found."),
                "MCP Feature Collection not found."
            },
            {
                unexpectedError.Object,
                "Unexpected mutation error: Something went wrong."
            }
        };
    }
}
