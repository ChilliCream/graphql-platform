using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Mcp;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class CreateMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "mcp",
                "create",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Creates a new MCP Feature Collection

            Usage:
              nitro mcp create [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --name <name>            The name of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_NAME]
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
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
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
    public async Task MissingRequiredName_ReturnsError(InteractionMode mode)
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
                "create",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "create",
                "--name",
                "my-mcp")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateMcpFeatureCollectionPayload("mcp-1", "my-mcp"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Creating MCP Feature Collection...
            └── ✓ Successfully created MCP Feature Collection!

            {
              "id": "mcp-1",
              "name": "my-mcp"
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateMcpFeatureCollectionPayload("mcp-1", "my-mcp"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
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
    public async Task MutationReturnsNullResult_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateMcpFeatureCollectionPayloadWithNullResult());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating MCP Feature Collection...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create MCP Feature Collection.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateMcpFeatureCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating MCP Feature Collection...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateMcpFeatureCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Creating MCP Feature Collection...
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateMcpFeatureCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
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
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("create failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: create failed
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
        mcpClient.Setup(x => x.CreateMcpFeatureCollectionAsync(
                "api-1",
                "my-mcp",
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
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-mcp")
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

    public static TheoryData<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors, string> CreateMutationErrorCases =>
        new()
        {
            {
                new CreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors_ApiNotFoundError(
                    "API not found", "ApiNotFoundError", "api-1"),
                """
                API not found
                """
            },
            {
                new CreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "Not authorized", "UnauthorizedOperation"),
                """
                Not authorized
                """
            }
        };
}
