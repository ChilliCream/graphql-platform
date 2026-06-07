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
              --client-id <client-id>  The ID of the client [env: NITRO_CLIENT_ID]
              --tag <tag>              The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage>          The name of the stage [env: NITRO_STAGE]
              --force                  Skip confirmation prompts for deletes and overwrites
              --wait-for-approval      Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro client publish \
                --client-id "<client-id>" \
                --tag "v1" \
                --stage "dev"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Publish_Should_ReturnError_When_ClientIdNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--client-id'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Publish_Should_ReturnError_When_StageNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--stage'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Publish_Should_ReturnError_When_TagNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--tag'.
            """);
        Assert.Equal(1, result.ExitCode);
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            └── ✕ Client version was rejected.
                └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
                    └── foo (10:10)
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client version was rejected.
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
            ├── ! Force push is enabled.
            ├── Publication request created. (ID: request-1)
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            ├── ! Failed validation.
            │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
            │       └── foo (10:10)
            ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            ├── Your request has been approved.
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
            Client version was rejected.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            ├── ! Failed validation.
            │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
            │       └── foo (10:10)
            ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            └── ✕ Client version was rejected.
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
            Publishing new version 'v1' of client 'client-1' to stage 'dev'
            ├── Publication request created. (ID: request-1)
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Publish_Should_PromptForTag_When_OnlyTagMissing_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        var command = StartInteractiveCommand(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

        // act
        command.Input(Tag); // Tag
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Publish_Should_PromptForStage_When_OnlyStageMissing_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupGetClientApiId();
        SetupListStagesQuery(("stage-1", Stage));
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        var command = StartInteractiveCommand(
            "client",
            "publish",
            "--client-id",
            ClientId,
            "--tag",
            Tag);

        // act
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Publish_Should_PromptForApiAndClient_When_OnlyClientMissing_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "publish",
            "--stage",
            Stage,
            "--tag",
            Tag);

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select client
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Publish_Should_PromptForStageAndTag_When_ClientProvided_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupGetClientApiId();
        SetupListStagesQuery(("stage-1", Stage));
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        var command = StartInteractiveCommand(
            "client",
            "publish",
            "--client-id",
            ClientId);

        // act
        command.Input(Tag);      // Tag
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Publish_Should_PromptForApiClientAndTag_When_StageProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "publish",
            "--stage",
            Stage);

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select client
        command.Input(Tag);      // Tag
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Publish_Should_PromptForApiClientAndStage_When_TagProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupListStagesQuery(("stage-1", Stage));
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "publish",
            "--tag",
            Tag);

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select client
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Publish_Should_PromptForApiClientStageAndTag_When_NothingProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupListStagesQuery(("stage-1", Stage));
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "publish");

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select client
        command.Input(Tag);      // Tag
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
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
