using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class CreatePersonalAccessTokenCommandTests
{
    [Fact]
    public async Task Create_InvalidExpires_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "pat",
            "create",
            "--description",
            "token for CI",
            "--expires",
            "invalid");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Cannot parse argument 'invalid' for option '--expires' as expected type 'System.Int32'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithOptions_JsonOutput_ReturnsTokenAndDetails()
    {
        // arrange
        var createdAt = new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero);
        var expiresAt = createdAt.AddDays(30);

        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.CreatePersonalAccessTokenAsync(
                "token for CI",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePatResult("secret-123", "pat-1", "token for CI", createdAt, expiresAt));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "pat",
            "create",
            "--description",
            "token for CI",
            "--expires",
            "30",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "pat-1",
                "description": "token for CI",
                "createdAt": "2026-03-27T00:00:00+00:00",
                "expiresAt": "2026-04-26T00:00:00+00:00"
              }
            }
            """);
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

    private static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken CreatePatResult(
        string secret,
        string id,
        string description,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        var token = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result_Token_PersonalAccessToken>();
        token.SetupGet(x => x.Id).Returns(id);
        token.SetupGet(x => x.Description).Returns(description);
        token.SetupGet(x => x.CreatedAt).Returns(createdAt);
        token.SetupGet(x => x.ExpiresAt).Returns(expiresAt);

        var result = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result_PersonalAccessTokenWithSecret>();
        result.SetupGet(x => x.Secret).Returns(secret);
        result.SetupGet(x => x.Token).Returns(token.Object);

        var payload = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken>();
        payload.SetupGet(x => x.Result).Returns(result.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
