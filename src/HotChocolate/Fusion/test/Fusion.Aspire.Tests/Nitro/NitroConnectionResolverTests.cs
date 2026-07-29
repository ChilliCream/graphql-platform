using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroConnectionResolverTests : IDisposable
{
    private static readonly Uri s_defaultApiUrl = new("https://api.chillicream.com");
    private static readonly DateTimeOffset s_now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly NitroTestDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task ResolveAsync_Should_UseTheDefaultApiUrl_When_NothingConfiguresOne()
    {
        // arrange
        var resolver = CreateResolver(_directory.GetPath("session.json"), new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("https://api.chillicream.com/", connection.ApiUrl.AbsoluteUri);
        Assert.Equal("https://api.chillicream.com/graphql", connection.GraphQLEndpoint.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_Should_UseTheAccessTokenApiUrl_When_TheTokenCarriesOne()
    {
        // arrange
        var sessionFilePath = WriteSession("nitro.example.com", s_now.AddHours(1));
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("https://nitro.example.com/", connection.ApiUrl.AbsoluteUri);
        Assert.Equal("https://nitro.example.com/graphql", connection.GraphQLEndpoint.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_Should_UseTheAccessTokenApiUrl_When_ItDiffersFromTheSessionValue()
    {
        // arrange
        var accessToken = CreateAccessToken("https://dedicated.example.com");
        var sessionFilePath = WriteSession(
            "stale.example.com",
            s_now.AddHours(1),
            accessToken);
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("https://dedicated.example.com/", connection.ApiUrl.AbsoluteUri);
        Assert.Equal(accessToken, connection.Credential.Value);
    }

    [Fact]
    public async Task ResolveAsync_Should_UseTheDefaultApiUrl_When_AnApiKeyIsSelected()
    {
        // arrange
        var sessionFilePath = WriteSession("nitro.example.com", s_now.AddHours(1));
        var environment = new TestNitroEnvironment(
            (NitroEnvironmentVariables.ApiKey, "nitro-api-key"));
        var resolver = CreateResolver(sessionFilePath, environment);

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("https://api.chillicream.com/", connection.ApiUrl.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_Should_UseTheDefaultApiUrl_When_TheAccessTokenHasNoApiUrlClaim()
    {
        // arrange
        var accessToken = CreateAccessToken(apiUrl: null);
        var sessionFilePath = WriteSession(
            "stale.example.com",
            s_now.AddHours(1),
            accessToken);
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("https://api.chillicream.com/", connection.ApiUrl.AbsoluteUri);
    }

    [Theory]
    [InlineData("opaque-access-token")]
    [InlineData("header.invalid-payload.signature")]
    public async Task ResolveAsync_Should_UseTheDefaultApiUrl_When_TheAccessTokenIsNotAValidJwt(
        string accessToken)
    {
        // arrange
        var sessionFilePath = WriteSession(
            "stale.example.com",
            s_now.AddHours(1),
            accessToken);
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("https://api.chillicream.com/", connection.ApiUrl.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_Should_PreferTheEnvironment_When_TheCloudUrlVariableIsSet()
    {
        // arrange
        var sessionFilePath = WriteSession("nitro.example.com", s_now.AddHours(1));
        var environment = new TestNitroEnvironment(
            (NitroEnvironmentVariables.CloudUrl, "http://localhost:5000"));
        var resolver = CreateResolver(sessionFilePath, environment);

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("http://localhost:5000/", connection.ApiUrl.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_Should_WarnAndFallBack_When_TheCloudUrlVariableIsNotAUrl()
    {
        // arrange
        var sessionFilePath = WriteSession("nitro.example.com", s_now.AddHours(1));
        var environment = new TestNitroEnvironment(
            (NitroEnvironmentVariables.CloudUrl, "https://"));
        var resolver = CreateResolver(sessionFilePath, environment);
        var logger = new RecordingLogger<NitroConnectionResolverTests>();

        // act
        var connection = await resolver.ResolveAsync(logger, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("https://nitro.example.com/", connection.ApiUrl.AbsoluteUri);
        Assert.Contains(
            logger.Entries,
            entry => entry.Message
                == "The value of the NITRO_CLOUD_URL environment variable is not a valid URL: "
                + "https://.");
    }

    [Fact]
    public async Task ResolveAsync_Should_PreferTheApiKey_When_TheSessionIsAlsoValid()
    {
        // arrange
        var sessionFilePath = WriteSession("nitro.example.com", s_now.AddHours(1));
        var environment = new TestNitroEnvironment(
            (NitroEnvironmentVariables.ApiKey, "nitro-api-key"));
        var resolver = CreateResolver(sessionFilePath, environment);

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroCredentialKind.ApiKey, connection.Credential.Kind);
        Assert.Equal("nitro-api-key", connection.Credential.Value);
    }

    [Fact]
    public async Task ResolveAsync_Should_UseTheAccessToken_When_TheTokenHasNotExpired()
    {
        // arrange
        var accessToken = CreateAccessToken("nitro.example.com");
        var sessionFilePath = WriteSession(
            "nitro.example.com",
            s_now.AddMinutes(1),
            accessToken);
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroCredentialKind.AccessToken, connection.Credential.Kind);
        Assert.Equal(accessToken, connection.Credential.Value);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    [InlineData(29)]
    public async Task ResolveAsync_Should_ReportExpired_When_TheTokenIsInsideTheGraceWindow(
        int secondsUntilExpiry)
    {
        // arrange
        // the grace window is 30 seconds, so a token that expires within it counts as expired
        var sessionFilePath = WriteSession(
            "nitro.example.com",
            s_now.AddSeconds(secondsUntilExpiry));
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroCredentialKind.None, connection.Credential.Kind);
        Assert.Equal(
            NitroCredentialUnavailableReason.SessionExpired,
            connection.Credential.UnavailableReason);
        Assert.Equal("https://api.chillicream.com/", connection.ApiUrl.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReportExpiredWithTheSessionPath_When_TheTokenExpired()
    {
        // arrange
        var sessionFilePath = WriteSession("nitro.example.com", s_now.AddHours(-1));
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            $"The Nitro session stored at '{sessionFilePath}' expired at 2026-07-29 11:00:00Z. "
            + "Run 'nitro login' to sign in again.",
            connection.Credential.UnavailableMessage);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReportMissing_When_NoSessionAndNoApiKeyExist()
    {
        // arrange
        var sessionFilePath = _directory.GetPath("session.json");
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            NitroCredentialUnavailableReason.SessionMissing,
            connection.Credential.UnavailableReason);
        Assert.Equal(
            $"No Nitro session file was found at '{sessionFilePath}'. Run 'nitro login' to sign "
            + "in, or set the NITRO_API_KEY environment variable.",
            connection.Credential.UnavailableMessage);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReportUnusable_When_TheSessionFileIsBroken()
    {
        // arrange
        var sessionFilePath = _directory.WriteFile("session.json", "{ broken");
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());

        // act
        var connection = await resolver.ResolveAsync(
            new RecordingLogger<NitroConnectionResolverTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            NitroCredentialUnavailableReason.SessionUnusable,
            connection.Credential.UnavailableReason);
        Assert.EndsWith(
            "Run 'nitro login' to sign in, or set the NITRO_API_KEY environment variable.",
            connection.Credential.UnavailableMessage);
    }

    [Fact]
    public async Task ResolveAsync_Should_LogTheApiUrlAndCredentialSource_When_ItResolves()
    {
        // arrange
        var sessionFilePath = WriteSession("nitro.example.com", s_now.AddHours(1));
        var resolver = CreateResolver(sessionFilePath, new TestNitroEnvironment());
        var logger = new RecordingLogger<NitroConnectionResolverTests>();

        // act
        await resolver.ResolveAsync(logger, TestContext.Current.CancellationToken);

        // assert
        Assert.Contains(
            logger.Entries,
            entry => entry.Message
                == "Using the Nitro API at https://nitro.example.com/ with the credential "
                + $"source {sessionFilePath}.");
    }

    private NitroConnectionResolver CreateResolver(
        string sessionFilePath,
        INitroEnvironment environment)
        => new(
            new NitroSessionReader(sessionFilePath, TimeSpan.Zero),
            environment,
            s_defaultApiUrl,
            new FakeTimeProvider(s_now),
            TimeSpan.FromSeconds(30));

    private string WriteSession(
        string apiUrl,
        DateTimeOffset expiresAt,
        string? accessToken = null)
    {
        accessToken ??= CreateAccessToken(apiUrl);

        return _directory.WriteFile(
            "session.json",
            $$"""
            {
              "apiUrl": "{{apiUrl}}",
              "email": "michael@chillicream.com",
              "tokens": {
                "accessToken": "{{accessToken}}",
                "expiresAt": "{{expiresAt.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}}"
              },
              "workspace": { "id": "workspace-1", "name": "Demo" }
            }
            """);
    }

    private static string CreateAccessToken(string? apiUrl)
    {
        var header = Base64UrlEncode("""{"alg":"RS256","typ":"JWT"}""");
        var claims = new Dictionary<string, string>
        {
            ["sub"] = "user-1",
            ["session_id"] = "session-1"
        };

        if (apiUrl is not null)
        {
            claims["api_url"] = apiUrl;
        }

        var payload = Base64UrlEncode(JsonSerializer.Serialize(claims));

        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
