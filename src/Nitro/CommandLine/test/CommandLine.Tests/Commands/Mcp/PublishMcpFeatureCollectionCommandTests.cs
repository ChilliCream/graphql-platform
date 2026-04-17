using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class PublishMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : McpCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish an MCP feature collection version to a stage.

            Usage:
              nitro mcp publish [options]

            Options:
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              --tag <tag> (REQUIRED)                                              The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)                                          The name of the stage [env: NITRO_STAGE]
              --force                                                             Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                                                 Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>                                             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                      Show help and usage information

            Example:
              nitro mcp publish \
                --mcp-feature-collection-id "<collection-id>" \
                --stage "dev" \
                --tag "v1"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ForceAndWaitForApproval_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--force",
            "--wait-for-approval");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The '--force' and '--wait-for-approval' options are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task StartMcpFeatureCollectionPublishThrows_ReturnsError()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetStartMcpFeatureCollectionPublishErrors))]
    public async Task StartMcpFeatureCollectionPublishHasErrors_ReturnsError(
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task StartMcpFeatureCollectionPublishReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ReturnsSuccess()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            └── ✓ Published new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_ReturnsSuccess()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(waitForApproval: true);
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            └── ✓ Published new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task BreakingChanges_ReturnsError()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            └── ✕ MCP feature collection version was rejected.
                └── Something went wrong during publish.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection version was rejected.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task BreakingChanges_Force_ReturnsSuccess()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(force: true);
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            ├── ! Force push is enabled.
            ├── Publication request created. (ID: request-1)
            └── ✓ Published new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_BreakingChanges_Approved_ReturnsSuccess()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(waitForApproval: true);
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishWaitForApprovalEventWithErrors(),
            CreateMcpFeatureCollectionPublishApprovedEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            ├── ! Failed validation.
            │   └── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       └── Tool 'Fail'
            │           └── The field `person` does not exist on the type `Query`. (1:14)
            ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            ├── Your request has been approved.
            └── ✓ Published new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_BreakingChanges_NotApproved_ReturnsError()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(waitForApproval: true);
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishWaitForApprovalEventWithErrors(),
            CreateMcpFeatureCollectionPublishFailedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--wait-for-approval");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection version was rejected.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            ├── ! Failed validation.
            │   └── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       └── Tool 'Fail'
            │           └── The field `person` does not exist on the type `Query`. (1:14)
            ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            └── ✕ MCP feature collection version was rejected.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.McpFeatureCollectionId, McpFeatureCollectionId);
        SetupEnvironmentVariable(EnvironmentVariables.Tag, Tag);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);

        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish");

        // assert
        result.AssertSuccess(
            """
            Publishing new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            └── ✓ Published new version 'v1' of MCP feature collection 'mcp-1' to stage 'dev'.
            """);
    }

    public static TheoryData<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors, string>
        GetStartMcpFeatureCollectionPublishErrors()
    {
        var unexpectedError = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        return new()
        {
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to publish."),
                "Not authorized to publish."
            },
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    Stage),
                "Stage not found."
            },
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                    McpFeatureCollectionId,
                    "MCP Feature Collection not found."),
                "MCP Feature Collection not found."
            },
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionVersionNotFoundError(
                    Tag,
                    "MCP Feature Collection version not found.",
                    McpFeatureCollectionId),
                "MCP Feature Collection version not found."
            },
            {
                unexpectedError.Object,
                "Unexpected mutation error: Something went wrong."
            }
        };
    }
}
