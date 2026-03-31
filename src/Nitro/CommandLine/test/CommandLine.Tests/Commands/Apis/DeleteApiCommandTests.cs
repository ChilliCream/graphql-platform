using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class DeleteApiCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "api",
                "delete",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete an API by ID.

            Usage:
              nitro api delete <id> [options]

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
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task ApiNotFound_ReturnsError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.GetApiForDeleteAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDeleteApiCommandQuery_Node?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The API with ID 'api-1' was not found.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.GetApiForDeleteAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateDeleteApiNode("my-api"));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "delete",
                "api-1")
            .Start();

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ? Do you really want to delete API my-api [y/n] (y): n
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The API was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.GetApiForDeleteAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateDeleteApiNode("my-api"));
        client.Setup(x => x.DeleteApiAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateDeleteApiPayload("api-1", "my-api", ["products"]));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Deleting API 'api-1'
            └── ✓ Deleted API 'api-1'.

            {
              "id": "api-1",
              "name": "my-api",
              "path": "products",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteApiMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IDeleteApiCommandMutation_DeleteApiById_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateDeleteMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the API.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteApiMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IDeleteApiCommandMutation_DeleteApiById_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateDeleteMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API 'api-1'
            └── ✕ Failed to delete the API.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteApiMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IDeleteApiCommandMutation_DeleteApiById_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateDeleteMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateDeleteExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the API.
            """);
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
        var client = CreateDeleteExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API 'api-1'
            └── ✕ Failed to delete the API.
            """);
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
        var client = CreateDeleteExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
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
        var client = CreateDeleteExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the API.
            """);
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
        var client = CreateDeleteExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting API 'api-1'
            └── ✕ Failed to delete the API.
            """);
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
        var client = CreateDeleteExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api",
                "delete",
                "api-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    public static TheoryData<IDeleteApiCommandMutation_DeleteApiById_Errors, string> DeleteApiMutationErrorCases =>
        new()
        {
            {
                new DeleteApiCommandMutation_DeleteApiById_Errors_ApiNotFoundError("API not found"),
                """
                Unexpected mutation error: API not found
                """
            },
            {
                new DeleteApiCommandMutation_DeleteApiById_Errors_UnauthorizedOperation("Not authorized"),
                """
                Unexpected mutation error: Not authorized
                """
            },
            {
                new DeleteApiCommandMutation_DeleteApiById_Errors_ApiDeletionFailedError("Deletion failed"),
                """
                Unexpected mutation error: Deletion failed
                """
            }
        };

    private static Mock<IApisClient> CreateDeleteMutationErrorClient(
        IDeleteApiCommandMutation_DeleteApiById_Errors mutationError)
    {
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.GetApiForDeleteAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateDeleteApiNode("my-api"));
        client.Setup(x => x.DeleteApiAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateDeleteApiPayloadWithErrors(mutationError));
        return client;
    }

    private static Mock<IApisClient> CreateDeleteExceptionClient(Exception ex)
    {
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.GetApiForDeleteAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateDeleteApiNode("my-api"));
        client.Setup(x => x.DeleteApiAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
