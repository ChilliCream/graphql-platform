using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class UnpublishClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Unpublish a client version from a stage.

            Usage:
              nitro client unpublish [options]

            Options:
              --tag <tag>              One or more client version tags to unpublish [env: NITRO_TAG]
              --stage <stage>          The name of the stage [env: NITRO_STAGE]
              --client-id <client-id>  The ID of the client [env: NITRO_CLIENT_ID]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro client unpublish \
                --client-id "<client-id>" \
                --stage "dev" \
                --tag "v1"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Unpublish_Should_ReturnError_When_ClientIdNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
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
    public async Task Unpublish_Should_ReturnError_When_StageNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
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
    public async Task Unpublish_Should_ReturnError_When_TagNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
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
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
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
    public async Task WithOptions_ReturnsSuccess()
    {
        // arrange
        SetupUnpublishClientMutation();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            Unpublishing client 'client-1' from stage 'dev'
            ├── Unpublishing tag 'v1'
            │   └── ✓ Unpublished tag 'v1'.
            └── ✓ Unpublished client 'client-1' from stage 'dev'.
            """);
    }

    [Fact]
    public async Task MultipleTags_ReturnsSuccess()
    {
        // arrange
        SetupUnpublishClientMutation("v1");
        SetupUnpublishClientMutation("v2");

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
            "--tag",
            "v1",
            "--tag",
            "v2",
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            Unpublishing client 'client-1' from stage 'dev'
            ├── Unpublishing tag 'v1'
            │   └── ✓ Unpublished tag 'v1'.
            ├── Unpublishing tag 'v2'
            │   └── ✓ Unpublished tag 'v2'.
            └── ✓ Unpublished client 'client-1' from stage 'dev'.
            """);
    }

    [Theory]
    [MemberData(nameof(UnpublishMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IUnpublishClient_UnpublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupUnpublishClientMutation(errors: [mutationError]);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Unpublishing client 'client-1' from stage 'dev'
            ├── Unpublishing tag 'v1'
            │   └── ✕ Failed to unpublish tag.
            └── ✕ Failed to unpublish the client.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UnpublishThrows_ReturnsError()
    {
        // arrange
        SetupUnpublishClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "unpublish",
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
            Unpublishing client 'client-1' from stage 'dev'
            ├── Unpublishing tag 'v1'
            │   └── ✕ Failed to unpublish tag.
            └── ✕ Failed to unpublish the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Unpublish_Should_PromptForTag_When_OnlyTagMissing_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupUnpublishClientMutation();

        var command = StartInteractiveCommand(
            "client",
            "unpublish",
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
    public async Task Unpublish_Should_PromptForStage_When_OnlyStageMissing_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupGetClientApiId();
        SetupListStagesQuery(("stage-1", Stage));
        SetupUnpublishClientMutation();

        var command = StartInteractiveCommand(
            "client",
            "unpublish",
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
    public async Task Unpublish_Should_PromptForApiAndClient_When_OnlyClientMissing_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupUnpublishClientMutation();

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "unpublish",
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
    public async Task Unpublish_Should_PromptForStageAndTag_When_ClientProvided_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupGetClientApiId();
        SetupListStagesQuery(("stage-1", Stage));
        SetupUnpublishClientMutation();

        var command = StartInteractiveCommand(
            "client",
            "unpublish",
            "--client-id",
            ClientId);

        // act
        command.SelectOption(0); // Select stage
        command.Input(Tag);      // Tag
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Unpublish_Should_PromptForApiClientAndTag_When_StageProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupUnpublishClientMutation();

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "unpublish",
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
    public async Task Unpublish_Should_PromptForApiClientAndStage_When_TagProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupListStagesQuery(("stage-1", Stage));
        SetupUnpublishClientMutation();

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "unpublish",
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
    public async Task Unpublish_Should_PromptForApiClientStageAndTag_When_NothingProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, ApiName));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupListStagesQuery(("stage-1", Stage));
        SetupUnpublishClientMutation();

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "unpublish");

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select client
        command.SelectOption(0); // Select stage
        command.Input(Tag);      // Tag
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    public static TheoryData<IUnpublishClient_UnpublishClient_Errors, string> UnpublishMutationErrorCases =>
        new()
        {
            {
                CreateUnpublishClientConcurrentOperationError(),
                """
                A concurrent operation is in progress.
                """
            },
            {
                CreateUnpublishClientStageNotFoundError(),
                """
                Stage 'dev' was not found.
                """
            },
            {
                CreateUnpublishClientVersionNotFoundError(),
                """
                Client version not found.
                """
            },
            {
                CreateUnpublishClientUnauthorizedError(),
                """
                Unauthorized.
                """
            },
            {
                CreateUnpublishClientClientNotFoundError(),
                """
                Client 'client-1' was not found.
                """
            }
        };
}
