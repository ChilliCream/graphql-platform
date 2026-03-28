using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class ListPersonalAccessTokenCommandTests
{
    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var createdAt = new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero);
        var expiresAt = createdAt.AddDays(90);

        var page = new ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>(
            [CreateTokenNode("pat-1", "token for CI", createdAt, expiresAt)],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "pat",
            "list",
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
                  "id": "pat-1",
                  "description": "token for CI",
                  "createdAt": "2026-03-27T00:00:00+00:00",
                  "expiresAt": "2026-06-25T00:00:00+00:00"
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
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>(
                    [],
                    EndCursor: null,
                    HasNextPage: false));

        var host = CreateHost(client);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync("pat", "list");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IPersonalAccessTokensClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node_PersonalAccessToken
        CreateTokenNode(
            string id,
            string description,
            DateTimeOffset createdAt,
            DateTimeOffset expiresAt)
    {
        var token = new Mock<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node_PersonalAccessToken>();
        token.SetupGet(x => x.Id).Returns(id);
        token.SetupGet(x => x.Description).Returns(description);
        token.SetupGet(x => x.CreatedAt).Returns(createdAt);
        token.SetupGet(x => x.ExpiresAt).Returns(expiresAt);

        return token.Object;
    }
}
