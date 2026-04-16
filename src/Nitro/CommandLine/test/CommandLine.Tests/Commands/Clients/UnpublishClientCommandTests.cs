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
              --tag <tag> (REQUIRED)              One or more client version tags to unpublish [env: NITRO_TAG]
              --stage <stage> (REQUIRED)          The name of the stage [env: NITRO_STAGE]
              --client-id <client-id> (REQUIRED)  The ID of the client [env: NITRO_CLIENT_ID]
              --cloud-url <cloud-url>             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information

            Example:
              nitro client unpublish \
                --client-id "<client-id>" \
                --stage "dev" \
                --tag "v1"
            """);
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
