using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class ListMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "mcp",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all MCP feature collections of an API.

            Usage:
              nitro mcp list [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --cursor <cursor>        The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
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
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually
            specify the '--workspace-id' option (if available).
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingApiId_ReturnsError(InteractionMode mode)
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
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("mcp-1", "auth-tools"),
                ("mcp-2", "data-tools")));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("mcp-1", "auth-tools"),
                ("mcp-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                },
                {
                  "id": "mcp-2",
                  "name": "data-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("mcp-1", "auth-tools"),
                ("mcp-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                },
                {
                  "id": "mcp-2",
                  "name": "data-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage());

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("mcp-1", "auth-tools")));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("mcp-1", "auth-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("mcp-1", "auth-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursorPagination_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: "cursor-2",
                hasNextPage: true,
                ("mcp-1", "auth-tools"),
                ("mcp-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                },
                {
                  "id": "mcp-2",
                  "name": "data-tools"
                }
              ],
              "cursor": "cursor-2"
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursorPagination_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        mcpClient.Setup(x => x.ListMcpFeatureCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpCommandTestHelper.CreateListPage(
                endCursor: "cursor-2",
                hasNextPage: true,
                ("mcp-1", "auth-tools"),
                ("mcp-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                },
                {
                  "id": "mcp-2",
                  "name": "data-tools"
                }
              ],
              "cursor": "cursor-2"
            }
            """);

        apisClient.VerifyAll();
        mcpClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
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
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
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
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
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
        var mcpClient = CreateListExceptionClient(new NitroClientAuthorizationException(), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
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

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = CreateListExceptionClient(new NitroClientAuthorizationException(), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
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

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mcpClient = CreateListExceptionClient(new NitroClientAuthorizationException(), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(mcpClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mcp",
                "list",
                "--api-id",
                "api-1")
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

    private static Mock<IMcpClient> CreateListExceptionClient(
        Exception ex,
        string apiId,
        string? cursor)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.ListMcpFeatureCollectionsAsync(
                apiId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
