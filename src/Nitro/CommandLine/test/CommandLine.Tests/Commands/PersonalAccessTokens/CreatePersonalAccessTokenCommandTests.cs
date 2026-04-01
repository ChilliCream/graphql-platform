using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class CreatePersonalAccessTokenCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "pat",
                "create",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new personal access token.

            Usage:
              nitro pat create [options]

            Options:
              --description <description>  The description of the personal access token [env: NITRO_DESCRIPTION]
              --expires <expires>          The expiration time of the personal access token in days [env: NITRO_EXPIRES] [default: 180]
              --cloud-url <cloud-url>      The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>          The API key used for authentication [env: NITRO_API_KEY]
              --output <json>              The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help               Show help and usage information
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
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredDescription_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "create")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--description'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePatPayload("pat-1", "my-token", "secret-123"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Creating personal access token
            └── ✓ Created personal access token 'my-token'.

            {
              "secret": "secret-123",
              "details": {
                "id": "pat-1",
                "description": "my-token",
                "createdAt": "2025-01-01T00:00:00+00:00",
                "expiresAt": "2025-06-01T00:00:00+00:00"
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePatPayload("pat-1", "my-token", "secret-123"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "pat-1",
                "description": "my-token",
                "createdAt": "2025-01-01T00:00:00+00:00",
                "expiresAt": "2025-06-01T00:00:00+00:00"
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullResult_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePatPayloadWithNullResult());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating personal access token
            └── ✕ Failed to create the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases_NonInteractive))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePatPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating personal access token
            └── ✕ Failed to create the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors mutationError,
        string expectedStdErr,
        InteractionMode mode)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePatPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
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
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
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
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating personal access token
            └── ✕ Failed to create the personal access token.
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
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "my-token",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "pat",
                "create",
                "--description",
                "my-token")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating personal access token
            └── ✕ Failed to create the personal access token.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    public static TheoryData<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors, string> CreateMutationErrorCases_NonInteractive =>
        new()
        {
            {
                new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation", "Not authorized"),
                """
                Not authorized
                """
            },
            {
                new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_ValidationError(
                    "Validation failed"),
                """
                Unexpected mutation error: Validation failed
                """
            }
        };

    public static TheoryData<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors, string, InteractionMode> CreateMutationErrorCases =>
        new()
        {
            {
                new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation", "Not authorized"),
                """
                Not authorized
                """,
                InteractionMode.Interactive
            },
            {
                new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_ValidationError(
                    "Validation failed"),
                """
                Unexpected mutation error: Validation failed
                """,
                InteractionMode.Interactive
            },
            {
                new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation", "Not authorized"),
                """
                Not authorized
                """,
                InteractionMode.JsonOutput
            },
            {
                new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_ValidationError(
                    "Validation failed"),
                """
                Unexpected mutation error: Validation failed
                """,
                InteractionMode.JsonOutput
            }
        };

    private static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken CreatePatPayload(
        string id, string description, string secret)
    {
        var token = new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result_Token_PersonalAccessToken(
            id,
            description,
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        var resultObj = new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result_PersonalAccessTokenWithSecret(
            token, secret);

        var payload = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.Result).Returns(resultObj);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken CreatePatPayloadWithNullResult()
    {
        var payload = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.Result)
            .Returns((ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken CreatePatPayloadWithErrors(
        params ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors[] errors)
    {
        var payload = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.Result)
            .Returns((ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }
}
