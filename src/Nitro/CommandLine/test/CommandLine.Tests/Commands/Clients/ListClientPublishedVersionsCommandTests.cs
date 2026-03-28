using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientPublishedVersionsCommandTests
{
    [Fact]
    public async Task ListPublishedVersions_MissingClientId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "list",
            "published-versions",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            The client ID is required in non-interactive mode.
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListPublishedVersions_NonInteractive_JsonOutput_ReturnsOnlyPublishedVersions()
    {
        // arrange
        var page = new ConnectionPage<IClientDetailPrompt_ClientVersionEdge>(
            [
                CreateClientVersionEdge(
                    "v1",
                    new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero),
                    ["prod", "staging"]),
                CreateClientVersionEdge(
                    "v2",
                    new DateTimeOffset(2025, 1, 3, 3, 4, 5, TimeSpan.Zero),
                    [])
            ],
            EndCursor: "cursor-2",
            HasNextPage: true);

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            "client-1",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "tag": "v1",
                  "createdAt": "2025-01-02T03:04:05+00:00",
                  "stages": [
                    "prod",
                    "staging"
                  ]
                }
              ],
              "cursor": "cursor-2"
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task ListPublishedVersions_InteractivePath_UsesPagedTableBranch()
    {
        // arrange
        var page = new ConnectionPage<IClientDetailPrompt_ClientVersionEdge>(
            [],
            EndCursor: null,
            HasNextPage: false);
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            "client-1");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IClientsClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IClientsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IClientDetailPrompt_ClientVersionEdge CreateClientVersionEdge(
        string tag,
        DateTimeOffset createdAt,
        params string[] stages)
    {
        var publishedTo = stages
            .Select(stageName =>
            {
                var stage = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node_PublishedTo_Stage_Stage>();
                stage.SetupGet(x => x.Name).Returns(stageName);

                var published = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node_PublishedTo_PublishedClientVersion>();
                published.SetupGet(x => x.Stage).Returns(stage.Object);

                return (IShowClientCommandQuery_Node_Versions_Edges_Node_PublishedTo)published.Object;
            })
            .ToArray();

        var node = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node_ClientVersion>();
        node.SetupGet(x => x.Id).Returns($"id-{tag}");
        node.SetupGet(x => x.Tag).Returns(tag);
        node.SetupGet(x => x.CreatedAt).Returns(createdAt);
        node.SetupGet(x => x.PublishedTo).Returns(publishedTo);

        var edge = new Mock<IClientDetailPrompt_ClientVersionEdge>();
        edge.SetupGet(x => x.Cursor).Returns($"cursor-{tag}");
        edge.SetupGet(x => x.Node).Returns(node.Object);

        return edge.Object;
    }
}
