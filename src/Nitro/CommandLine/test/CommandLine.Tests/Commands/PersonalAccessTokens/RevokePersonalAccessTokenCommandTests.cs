using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class RevokePersonalAccessTokenCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "pat",
                "revoke",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Revoke a personal access token.

            Usage:
              nitro pat revoke <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task ConfirmationRejected_ReturnsError_Interactive()
    {
        // arrange
        var command = new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1")
            .Start();

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            PAT was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithConfirmation_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePayload("pat-1", "my-token"));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePayload("pat-1", "my-token"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Revoking personal access token 'pat-1'
            └── ✓ Revoked personal access token 'pat-1'.

            {
              "id": "pat-1",
              "description": "my-token",
              "createdAt": "2025-01-01T00:00:00+00:00",
              "expiresAt": "2025-06-01T00:00:00+00:00"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePayload("pat-1", "my-token"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "pat-1",
              "description": "my-token",
              "createdAt": "2025-01-01T00:00:00+00:00",
              "expiresAt": "2025-06-01T00:00:00+00:00"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullResult_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePayloadWithNullResult());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Revoking personal access token 'pat-1'
            └── ✕ Failed to revoke the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not revoke personal access token.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(RevokeMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Revoking personal access token 'pat-1'
            └── ✕ Failed to revoke the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(RevokeMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePayloadWithErrors(mutationError));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(RevokeMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Revoking personal access token 'pat-1'
            └── ✕ Failed to revoke the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync(
                "pat-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "revoke",
                "pat-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Revoking personal access token 'pat-1'
            └── ✕ Failed to revoke the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    public static TheoryData<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors, string> RevokeMutationErrorCases =>
        new()
        {
            {
                new RevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors_PersonalAccessTokenNotFoundError(
                    "PersonalAccessTokenNotFoundError", "PAT not found"),
                """
                PAT not found
                """
            },
            {
                new RevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors_UnauthorizedOperation(
                    "Not authorized"),
                """
                Unexpected mutation error: Not authorized
                """
            }
        };

    private static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken CreateRevokePayload(
        string id, string description)
    {
        var token = new RevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_PersonalAccessToken_PersonalAccessToken(
            id,
            description,
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        var payload = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.PersonalAccessToken).Returns(token);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken CreateRevokePayloadWithNullResult()
    {
        var payload = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.PersonalAccessToken)
            .Returns((IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_PersonalAccessToken?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken CreateRevokePayloadWithErrors(
        params IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors[] errors)
    {
        var payload = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.PersonalAccessToken)
            .Returns((IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_PersonalAccessToken?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }
}
