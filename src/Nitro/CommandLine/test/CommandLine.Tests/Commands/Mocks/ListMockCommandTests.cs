using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class ListMockCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "mock",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all mock schemas in an API.

            Usage:
              nitro mock list [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
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
                "mock",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);

        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingApiId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);

        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        mocksClient.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListMockSchemasPage(
                endCursor: null,
                hasNextPage: false,
                ("mock-1", "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                    "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                    "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero)),
                ("mock-2", "Mock Two", "https://mock.example.com/2", new Uri("https://downstream.example.com/2"),
                    "user3", new DateTimeOffset(2025, 2, 10, 10, 0, 0, TimeSpan.Zero),
                    "user4", new DateTimeOffset(2025, 2, 11, 10, 0, 0, TimeSpan.Zero))));

        var command = new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
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
        mocksClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        mocksClient.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListMockSchemasPage(
                endCursor: null,
                hasNextPage: false,
                ("mock-1", "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                    "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                    "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero)),
                ("mock-2", "Mock Two", "https://mock.example.com/2", new Uri("https://downstream.example.com/2"),
                    "user3", new DateTimeOffset(2025, 2, 10, 10, 0, 0, TimeSpan.Zero),
                    "user4", new DateTimeOffset(2025, 2, 11, 10, 0, 0, TimeSpan.Zero))));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
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
                  "id": "mock-1",
                  "name": "Mock One",
                  "url": "https://mock.example.com/1",
                  "downstreamUrl": "https://downstream.example.com/1",
                  "createdBy": {
                    "username": "user1",
                    "createdAt": "2025-01-15T10:00:00+00:00"
                  },
                  "modifiedBy": {
                    "username": "user2",
                    "modifiedAt": "2025-01-16T10:00:00+00:00"
                  }
                },
                {
                  "id": "mock-2",
                  "name": "Mock Two",
                  "url": "https://mock.example.com/2",
                  "downstreamUrl": "https://downstream.example.com/2",
                  "createdBy": {
                    "username": "user3",
                    "createdAt": "2025-02-10T10:00:00+00:00"
                  },
                  "modifiedBy": {
                    "username": "user4",
                    "modifiedAt": "2025-02-11T10:00:00+00:00"
                  }
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        mocksClient.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListMockSchemasPage(
                endCursor: "cursor-2",
                hasNextPage: true,
                ("mock-1", "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                    "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                    "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero))));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
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
                  "id": "mock-1",
                  "name": "Mock One",
                  "url": "https://mock.example.com/1",
                  "downstreamUrl": "https://downstream.example.com/1",
                  "createdBy": {
                    "username": "user1",
                    "createdAt": "2025-01-15T10:00:00+00:00"
                  },
                  "modifiedBy": {
                    "username": "user2",
                    "modifiedAt": "2025-01-16T10:00:00+00:00"
                  }
                }
              ],
              "cursor": "cursor-2"
            }
            """);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        mocksClient.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListMockSchemasPage());

        var command = new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
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
        mocksClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        mocksClient.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListMockSchemasPage());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
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
        mocksClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        mocksClient.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListMockSchemasPage(
                endCursor: null,
                hasNextPage: false,
                ("mock-1", "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                    "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                    "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero))));

        var command = new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
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
        mocksClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = CreateListExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = CreateListExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
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
        mocksClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = CreateListExceptionClient(
            new NitroClientAuthorizationException(), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = CreateListExceptionClient(
            new NitroClientAuthorizationException(), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    private static ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node> CreateListMockSchemasPage(
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name, string Url, Uri DownstreamUrl,
            string CreatedByUsername, DateTimeOffset CreatedAt,
            string ModifiedByUsername, DateTimeOffset ModifiedAt)[] mocks)
    {
        var items = mocks
            .Select(static m => CreateMockSchemaNode(
                m.Id, m.Name, m.Url, m.DownstreamUrl,
                m.CreatedByUsername, m.CreatedAt,
                m.ModifiedByUsername, m.ModifiedAt))
            .ToArray();

        return new ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(items, endCursor, hasNextPage);
    }

    private static IListMockCommandQuery_ApiById_MockSchemas_Edges_Node CreateMockSchemaNode(
        string id,
        string name,
        string url,
        Uri downstreamUrl,
        string createdByUsername,
        DateTimeOffset createdAt,
        string modifiedByUsername,
        DateTimeOffset modifiedAt)
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy>(MockBehavior.Strict);
        createdBy.SetupGet(x => x.Username).Returns(createdByUsername);

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy>(MockBehavior.Strict);
        modifiedBy.SetupGet(x => x.Username).Returns(modifiedByUsername);

        var node = new Mock<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(MockBehavior.Strict);
        node.SetupGet(x => x.Id).Returns(id);
        node.SetupGet(x => x.Name).Returns(name);
        node.SetupGet(x => x.Url).Returns(url);
        node.SetupGet(x => x.DownstreamUrl).Returns(downstreamUrl);
        node.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        node.SetupGet(x => x.CreatedAt).Returns(createdAt);
        node.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);
        node.SetupGet(x => x.ModifiedAt).Returns(modifiedAt);

        return node.Object;
    }

    private static Mock<IMocksClient> CreateListExceptionClient(
        Exception ex,
        string apiId,
        string? cursor)
    {
        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        client.Setup(x => x.ListMockSchemasAsync(
                apiId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
