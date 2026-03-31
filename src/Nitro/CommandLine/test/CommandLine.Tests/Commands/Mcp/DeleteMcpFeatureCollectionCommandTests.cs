using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class DeleteMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "mcp",
                "delete",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete an MCP feature collection.

            Usage:
              nitro mcp delete [<id>] [options]

            Arguments:
              <id>  The ID

            Options:
              --force                  Will not ask for confirmation on deletes or overwrites.
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The output format. Setting this option will disable the interactive mode. [env: NITRO_OUTPUT_FORMAT]
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
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "delete",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The 'id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1")
            .Start();

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The MCP Feature Collection was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateDeleteMcpFeatureCollectionPayload("mcp-1", "my-mcp"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✓ Deleted MCP feature collection 'mcp-1'.

            {
              "id": "mcp-1",
              "name": "my-mcp"
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateDeleteMcpFeatureCollectionPayload("mcp-1", "my-mcp"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "mcp-1",
              "name": "my-mcp"
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithConfirmation_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateDeleteMcpFeatureCollectionPayload("mcp-1", "my-mcp"));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullResult_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateDeleteMcpFeatureCollectionPayloadWithNullResult());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✕ Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateDeleteMcpFeatureCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✕ Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateDeleteMcpFeatureCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateDeleteMcpFeatureCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✕ Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✕ Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    public static TheoryData<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors, string> DeleteMutationErrorCases =>
        new()
        {
            {
                new DeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors_McpFeatureCollectionNotFoundError(
                    "MCP Feature Collection not found", "mcp-1"),
                """
                MCP Feature Collection not found
                """
            },
            {
                new DeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors_UnauthorizedOperation(
                    "Not authorized", "UnauthorizedOperation"),
                """
                Not authorized
                """
            }
        };
}
