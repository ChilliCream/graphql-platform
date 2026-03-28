using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class ListMockCommandTests
{
    [Fact]
    public async Task List_MissingApiId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "mock",
            "list",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            The API ID is required in non-interactive mode.

            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var createdAt = new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero);
        var page = new ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(
            [CreateMockNode(
                "mock-1",
                "mock-schema",
                "https://mock.local/graphql",
                "https://downstream.local/graphql",
                "alice",
                createdAt,
                "bob",
                createdAt.AddHours(1))],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        client.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "mock",
            "list",
            "--api-id",
            "api-1",
            "--cursor",
            "cursor-start",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "id": "mock-1",
                  "name": "mock-schema",
                  "url": "https://mock.local/graphql",
                  "downstreamUrl": "https://downstream.local/graphql",
                  "createdBy": {
                    "username": "alice",
                    "createdAt": "2026-03-27T00:00:00+00:00"
                  },
                  "modifiedBy": {
                    "username": "bob",
                    "modifiedAt": "2026-03-27T01:00:00+00:00"
                  }
                }
              ],
              "cursor": "cursor-1"
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task List_InteractivePath_UsesPagedTableBranch()
    {
        // arrange
        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        client.Setup(x => x.ListMockSchemasAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(
                [],
                EndCursor: null,
                HasNextPage: false));

        var host = CreateHost(client);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "mock",
            "list",
            "--api-id",
            "api-1");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IMocksClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IListMockCommandQuery_ApiById_MockSchemas_Edges_Node_MockSchema CreateMockNode(
        string id,
        string name,
        string url,
        string downstreamUrl,
        string createdByUsername,
        DateTimeOffset createdAt,
        string modifiedByUsername,
        DateTimeOffset modifiedAt)
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy_UserInfo>();
        createdBy.SetupGet(x => x.Username).Returns(createdByUsername);

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy_UserInfo>();
        modifiedBy.SetupGet(x => x.Username).Returns(modifiedByUsername);

        var mockSchema = new Mock<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node_MockSchema>();
        mockSchema.SetupGet(x => x.Id).Returns(id);
        mockSchema.SetupGet(x => x.Name).Returns(name);
        mockSchema.SetupGet(x => x.Url).Returns(url);
        mockSchema.SetupGet(x => x.DownstreamUrl).Returns(new Uri(downstreamUrl));
        mockSchema.SetupGet(x => x.CreatedAt).Returns(createdAt);
        mockSchema.SetupGet(x => x.ModifiedAt).Returns(modifiedAt);
        mockSchema.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        mockSchema.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);

        return mockSchema.Object;
    }
}
