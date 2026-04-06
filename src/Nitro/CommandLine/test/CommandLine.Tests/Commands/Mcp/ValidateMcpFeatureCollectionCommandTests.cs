using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
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
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task StartMcpFeatureCollectionValidationThrows_ReturnsError()
    {
        // arrange
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
            Validating MCP feature collection against stage 'dev'
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
            Validating MCP feature collection against stage 'dev'
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
            Validating MCP feature collection against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess()
    {
        // arrange
        SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionValidationOperationInProgressEvent(),
            CreateMcpFeatureCollectionValidationInProgressEvent(),
            CreateMcpFeatureCollectionValidationSuccessEvent());

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
        result.AssertSuccess(
            """
            Validating MCP feature collection against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Validated MCP feature collection against stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        var errorMock = new Mock<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionValidationOperationInProgressEvent(),
            CreateMcpFeatureCollectionValidationFailedEvent(errorMock.Object));

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
            Validating MCP feature collection against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            │       └── Something went wrong during validation.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionValidationOperationInProgressEvent());

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
            Validating MCP feature collection against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        var unknownEvent = new Mock<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription(unknownEvent.Object);

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
            Validating MCP feature collection against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── ! Unknown server response. Consider updating the CLI.
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_FailedWithValidationError_ReturnsError()
    {
        // arrange
        var validationError = new Mock<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        validationError.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(Array.Empty<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections>());

        SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionValidationOperationInProgressEvent(),
            CreateMcpFeatureCollectionValidationFailedEvent(validationError.Object));

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
            Validating MCP feature collection against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_FailedWithTimeoutError_ReturnsError()
    {
        // arrange
        var timeoutError = new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors_ProcessingTimeoutError(
            "ProcessingTimeoutError",
            "The validation timed out.");

        SetupValidateMcpFeatureCollectionMutation();
        SetupValidateMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionValidationOperationInProgressEvent(),
            CreateMcpFeatureCollectionValidationFailedEvent(timeoutError));

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
            Validating MCP feature collection against stage 'dev'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            │       └── The validation timed out.
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
