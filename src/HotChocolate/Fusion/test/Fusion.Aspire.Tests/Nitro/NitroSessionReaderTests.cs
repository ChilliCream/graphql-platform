namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroSessionReaderTests : IDisposable
{
    private readonly NitroTestDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task ReadAsync_Should_ReturnTheSession_When_TheFileIsWrittenByTheNitroCli()
    {
        // arrange
        // the shape and the camelCase naming match what the Nitro CLI serializes
        var path = _directory.WriteFile(
            "session.json",
            """
            {
              "sessionId": "session-1",
              "subjectId": "subject-1",
              "tenant": "tenant-1",
              "identityServer": "https://identity.chillicream.com",
              "apiUrl": "api.chillicream.com",
              "email": "michael@chillicream.com",
              "tokens": {
                "accessToken": "access-token",
                "idToken": "id-token",
                "refreshToken": "refresh-token",
                "expiresAt": "2026-07-29T10:00:00+00:00"
              },
              "workspace": {
                "id": "workspace-1",
                "name": "Demo"
              }
            }
            """);
        var reader = new NitroSessionReader(path, TimeSpan.Zero);

        // act
        var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSessionStatus.Available, result.Status);
        Assert.Equal("api.chillicream.com", result.Session!.ApiUrl);
        Assert.Equal("https://identity.chillicream.com", result.Session.IdentityServer);
        Assert.Equal("access-token", result.Session.Tokens!.AccessToken);
        Assert.Equal("id-token", result.Session.Tokens.IdToken);
        Assert.Equal("refresh-token", result.Session.Tokens.RefreshToken);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 29, 10, 0, 0, TimeSpan.Zero),
            result.Session.Tokens.ExpiresAt);
    }

    [Fact]
    public async Task ReadAsync_Should_IgnoreUnknownMembers_When_TheFileCarriesNewMembers()
    {
        // arrange
        var path = _directory.WriteFile(
            "session.json",
            """
            {
              "apiUrl": "api.chillicream.com",
              "email": "michael@chillicream.com",
              "somethingNew": { "nested": [1, 2, 3] },
              "tokens": {
                "accessToken": "access-token",
                "expiresAt": "2026-07-29T10:00:00+00:00",
                "somethingElseNew": true
              }
            }
            """);
        var reader = new NitroSessionReader(path, TimeSpan.Zero);

        // act
        var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSessionStatus.Available, result.Status);
        Assert.Equal("access-token", result.Session!.Tokens!.AccessToken);
    }

    [Fact]
    public async Task ReadAsync_Should_ReportMissing_When_NoSessionFileExists()
    {
        // arrange
        var path = _directory.GetPath("session.json");
        var reader = new NitroSessionReader(path, TimeSpan.Zero);

        // act
        var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSessionStatus.Missing, result.Status);
        Assert.Null(result.Session);
        Assert.Equal($"No Nitro session file was found at '{path}'.", result.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ReportUnusableWithThePath_When_TheFileIsNotValidJson()
    {
        // arrange
        var path = _directory.WriteFile("session.json", "{ this is not json");
        var reader = new NitroSessionReader(path, TimeSpan.Zero);

        // act
        var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSessionStatus.Unusable, result.Status);
        Assert.StartsWith(
            $"The Nitro session file '{path}' could not be parsed:",
            result.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ReportUnusable_When_TheFileCarriesNoAccessToken()
    {
        // arrange
        var path = _directory.WriteFile(
            "session.json",
            """
            {
              "apiUrl": "api.chillicream.com",
              "tokens": { "expiresAt": "2026-07-29T10:00:00+00:00" }
            }
            """);
        var reader = new NitroSessionReader(path, TimeSpan.Zero);

        // act
        var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSessionStatus.Unusable, result.Status);
        Assert.Equal(
            $"The Nitro session file '{path}' carries no access token.",
            result.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ReportUnusable_When_TheExpiryIsMissing()
    {
        // arrange
        var path = _directory.WriteFile(
            "session.json",
            """
            {
              "apiUrl": "api.chillicream.com",
              "tokens": { "accessToken": "access-token" }
            }
            """);
        var reader = new NitroSessionReader(path, TimeSpan.Zero);

        // act
        var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSessionStatus.Unusable, result.Status);
        Assert.Equal(
            $"The Nitro session file '{path}' carries no access token expiry.",
            result.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ReportUnusable_When_TheExpiryCannotBeParsed()
    {
        // arrange
        var path = _directory.WriteFile(
            "session.json",
            """
            {
              "tokens": { "accessToken": "access-token", "expiresAt": "not-a-date" }
            }
            """);
        var reader = new NitroSessionReader(path, TimeSpan.Zero);

        // act
        var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSessionStatus.Unusable, result.Status);
        Assert.StartsWith(
            $"The Nitro session file '{path}' could not be parsed:",
            result.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_SucceedOnTheSecondRead_When_TheFirstReadRacedWithALogin()
    {
        // arrange
        // the first read observes a half written document, the re-read observes the final one
        var path = _directory.WriteFile("session.json", "{ \"tokens\": { \"accessTo");
        var reader = new NitroSessionReader(path, TimeSpan.FromSeconds(1));

        // act
        var readTask = reader.ReadAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        File.WriteAllText(
            path,
            """
            { "tokens": { "accessToken": "access-token", "expiresAt": "2026-07-29T10:00:00Z" } }
            """);
        var result = await readTask;

        // assert
        Assert.Equal(NitroSessionStatus.Available, result.Status);
        Assert.Equal("access-token", result.Session!.Tokens!.AccessToken);
    }
}
