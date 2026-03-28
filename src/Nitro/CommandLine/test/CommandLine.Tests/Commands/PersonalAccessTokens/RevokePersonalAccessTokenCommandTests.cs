using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class RevokePersonalAccessTokenCommandTests
{
    [Fact]
    public async Task Revoke_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("pat", "revoke", "--force");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'revoke'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Revoke_WithForce_JsonOutput_ReturnsRevokedToken()
    {
        // arrange
        var createdAt = new DateTimeOffset(2026, 03, 01, 0, 0, 0, TimeSpan.Zero);
        var expiresAt = new DateTimeOffset(2026, 06, 01, 0, 0, 0, TimeSpan.Zero);
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.RevokePersonalAccessTokenAsync("pat-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTokenResult("pat-1", "token for CI", createdAt, expiresAt));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "pat",
            "revoke",
            "pat-1",
            "--force",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "pat-1",
              "description": "token for CI",
              "createdAt": "2026-03-01T00:00:00+00:00",
              "expiresAt": "2026-06-01T00:00:00+00:00"
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandBuilder CreateHost(
        Mock<IPersonalAccessTokensClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken CreateTokenResult(
        string id,
        string description,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        var token = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_PersonalAccessToken_PersonalAccessToken>();
        token.SetupGet(x => x.Id).Returns(id);
        token.SetupGet(x => x.Description).Returns(description);
        token.SetupGet(x => x.CreatedAt).Returns(createdAt);
        token.SetupGet(x => x.ExpiresAt).Returns(expiresAt);

        var payload = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken>();
        payload.SetupGet(x => x.PersonalAccessToken).Returns(token.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
