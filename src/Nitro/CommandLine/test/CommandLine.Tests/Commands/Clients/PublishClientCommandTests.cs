using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class PublishClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a client version to a stage.

            Usage:
              nitro client publish [options]

            Options:
              --client-id <client-id> (REQUIRED)  The ID of the client [env: NITRO_CLIENT_ID]
              --tag <tag> (REQUIRED)              The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)          The name of the stage [env: NITRO_STAGE]
              --force                             Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                 Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information

            Example:
              nitro client publish \
                --client-id "<client-id>" \
                --tag "v1" \
                --stage "dev"
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
            "client",
            "publish",
            "--client-id",
            ClientId,
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
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task PublishThrows_ReturnsError()
    {
        // arrange
        SetupPublishClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetMutationErrors))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IPublishClientVersion_PublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupPublishClientMutation(errors: mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupPublishClientMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ReturnsSuccess()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag,
            "--stage",
            Stage);

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_NoBreakingChanges_ReturnsSuccess()
    {
        // arrange
        SetupPublishClientMutation(waitForApproval: true);
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task BreakingChanges_ReturnsError()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag,
            "--stage",
            Stage);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✕ Processing failed.
            │       └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
            │           └── foo (10:10)
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task BreakingChanges_Force_ReturnsSuccess()
    {
        // arrange
        SetupPublishClientMutation(force: true);
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── ! Force push is enabled.
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_BreakingChanges_Approved_ReturnsSuccess()
    {
        // arrange
        SetupPublishClientMutation(waitForApproval: true);
        SetupPublishClientSubscription(
            CreateClientVersionPublishWaitForApprovalEventWithErrors(),
            CreateClientVersionPublishApprovedEvent(),
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── ! Validation failed.
            │   │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
            │   │       └── foo (10:10)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            │   ├── Your request has been approved.
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_BreakingChanges_NotApproved_ReturnsError()
    {
        // arrange
        SetupPublishClientMutation(waitForApproval: true);
        SetupPublishClientSubscription(
            CreateClientVersionPublishWaitForApprovalEventWithErrors(),
            CreateClientVersionPublishFailedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--wait-for-approval");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Client publish failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── ! Validation failed.
            │   │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
            │   │       └── foo (10:10)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ClientId, ClientId);
        SetupEnvironmentVariable(EnvironmentVariables.Tag, Tag);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);

        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish");

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    public static TheoryData<IPublishClientVersion_PublishClient_Errors, string> GetMutationErrors()
    {
        var unexpectedError = new Mock<IPublishClientVersion_PublishClient_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        return new()
        {
            {
                new PublishClientVersion_PublishClient_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to publish."),
                "Not authorized to publish."
            },
            {
                new PublishClientVersion_PublishClient_Errors_ClientNotFoundError(
                    "Client not found.",
                    "client-1"),
                "Client not found."
            },
            {
                new PublishClientVersion_PublishClient_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    "dev"),
                "Stage not found."
            },
            {
                new PublishClientVersion_PublishClient_Errors_ClientVersionNotFoundError(
                    "v1",
                    "Client version not found.",
                    "client-1"),
                "Client version not found."
            },
            {
                new PublishClientVersion_PublishClient_Errors_InvalidSourceMetadataInputError(
                    "InvalidSourceMetadataInputError",
                    "Invalid source metadata."),
                "Invalid source metadata."
            },
            {
                unexpectedError.Object,
                "Unexpected mutation error: Something went wrong."
            }
        };
    }
}
