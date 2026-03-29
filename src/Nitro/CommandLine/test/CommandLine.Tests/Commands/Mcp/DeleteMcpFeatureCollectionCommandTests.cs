using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Exceptions;
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
              Deletes an MCP Feature Collection

            Usage:
              nitro mcp delete [<id>] [options]

            Arguments:
              <id>  The ID

            Options:
              --force                  Will not ask for confirmation on deletes or overwrites.
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
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
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
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
            Deleting MCP Feature Collection...
            └── Successfully deleted MCP Feature Collection!

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
            Deleting MCP Feature Collection...
            └── Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not delete the MCP Feature Collection.
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
            Deleting MCP Feature Collection...
            └── Failed!
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

            [    ] Deleting MCP Feature Collection...
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

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("delete failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: delete failed
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.DeleteMcpFeatureCollectionAsync(
                "mcp-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "delete",
                "mcp-1",
                "--force")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

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
