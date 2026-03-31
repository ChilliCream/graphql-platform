using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class UnpublishClientCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "unpublish",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishSuccessPayload("my-client"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Unpublishing client 'client-1' from stage 'production'
            ├── Unpublishing v1...
            Unpublished my-client:v1 from production
            └── ✓ Unpublished client 'client-1' from stage 'production'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishSuccessPayload("my-client"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MultipleTags_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishSuccessPayload("my-client"));
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v2",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishSuccessPayload("my-client"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--tag",
                "v2",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Unpublishing client 'client-1' from stage 'production'
            ├── Unpublishing v1...
            Unpublished my-client:v1 from production
            ├── Unpublishing v2...
            Unpublished my-client:v2 from production
            └── ✓ Unpublished client 'client-1' from stage 'production'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UnpublishMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IUnpublishClient_UnpublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Unpublishing client 'client-1' from stage 'production'
            ├── Unpublishing v1...
            └── ✕ Failed to unpublish the client.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UnpublishMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IUnpublishClient_UnpublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to unpublish the client.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UnpublishMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IUnpublishClient_UnpublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
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
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
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
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
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
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
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
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "production",
                "v1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "unpublish",
                "--tag",
                "v1",
                "--stage",
                "production",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    public static TheoryData<IUnpublishClient_UnpublishClient_Errors, string> UnpublishMutationErrorCases =>
        new()
        {
            {
                new UnpublishClient_UnpublishClient_Errors_ConcurrentOperationError("ConcurrentOperationError", "Concurrent operation in progress"),
                """
                Concurrent operation in progress
                """
            },
            {
                new UnpublishClient_UnpublishClient_Errors_StageNotFoundError("StageNotFoundError", "Stage not found", "production"),
                """
                Stage not found
                """
            },
            {
                new UnpublishClient_UnpublishClient_Errors_ClientVersionNotFoundError("v1", "Client version not found", "client-1"),
                """
                Client version not found
                """
            },
            {
                new UnpublishClient_UnpublishClient_Errors_UnauthorizedOperation("UnauthorizedOperation", "Not authorized"),
                """
                Not authorized
                """
            },
            {
                new UnpublishClient_UnpublishClient_Errors_ClientNotFoundError("Client not found", "client-1"),
                """
                Client not found
                """
            }
        };

    private static IUnpublishClient_UnpublishClient CreateUnpublishSuccessPayload(string clientName)
    {
        var clientObj = new Mock<IUnpublishClient_UnpublishClient_ClientVersion_Client>(MockBehavior.Strict);
        clientObj.SetupGet(x => x.Name).Returns(clientName);

        var clientVersion = new Mock<IUnpublishClient_UnpublishClient_ClientVersion>(MockBehavior.Strict);
        clientVersion.SetupGet(x => x.Id).Returns("cv-1");
        clientVersion.SetupGet(x => x.Client).Returns(clientObj.Object);

        var payload = new Mock<IUnpublishClient_UnpublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.ClientVersion).Returns(clientVersion.Object);
        payload.SetupGet(x => x.Errors).Returns((IReadOnlyList<IUnpublishClient_UnpublishClient_Errors>?)null);

        return payload.Object;
    }

    private static IUnpublishClient_UnpublishClient CreateUnpublishPayloadWithErrors(
        params IUnpublishClient_UnpublishClient_Errors[] errors)
    {
        var payload = new Mock<IUnpublishClient_UnpublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.ClientVersion).Returns((IUnpublishClient_UnpublishClient_ClientVersion?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);

        return payload.Object;
    }
}
