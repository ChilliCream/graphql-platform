using System.Net;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class OpencodeServerClientTests
{
    [Fact]
    public async Task PushMessageAsync_Should_PreserveTextAndMarkTheTextPart_When_ServerAcceptsTheMessage()
    {
        // arrange: the caller, not this client, prepends the reserved
        // OpencodeHookProtocol.PushedPromptPrefix to text when it wants the
        // turn recognized as Nitro-pushed - this client sends text verbatim.
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var client = CreateClient(async (request, _) =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        const string text = OpencodeHookProtocol.PushedPromptPrefix + "user-authored text";

        // act
        var result = await client.PushMessageAsync(
            "http://localhost:4096", "ses_123", text, secret: null, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, result);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/session/ses_123/message", capturedRequest.RequestUri!.AbsolutePath);
        using var body = JsonDocument.Parse(capturedBody!);
        Assert.Equal("text", body.RootElement.GetProperty("parts")[0].GetProperty("type").GetString());
        Assert.Equal(text, body.RootElement.GetProperty("parts")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task PushMessageAsync_Should_SendBasicAuthentication_When_SecretIsPresent()
    {
        // arrange
        HttpRequestMessage? capturedRequest = null;
        var client = CreateClient((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // act
        await client.PushMessageAsync(
            "http://localhost:4096",
            "ses_123",
            "digest",
            "secret-value",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("Basic", capturedRequest!.Headers.Authorization!.Scheme);
        var encodedCredentials = capturedRequest.Headers.Authorization.Parameter!;
        Assert.Equal("opencode:secret-value", Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials)));
    }

    [Fact]
    public async Task PingAsync_Should_ReturnOk_When_HealthAndSessionAreAvailable()
    {
        // arrange
        var paths = new List<string>();
        var client = CreateClient((request, _) =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // act
        var result = await client.PingAsync(
            "http://localhost:4096",
            "ses_123",
            secret: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, result);
        Assert.Equal(["/global/health", "/session/ses_123"], paths);
    }

    [Fact]
    public async Task PingAsync_Should_ReturnEndpointGone_When_SessionDoesNotExist()
    {
        // arrange
        var client = CreateClient((request, _) => Task.FromResult(
            new HttpResponseMessage(
                request.RequestUri!.AbsolutePath == "/session/ses_missing"
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.OK)));

        // act
        var result = await client.PingAsync(
            "http://localhost:4096",
            "ses_missing",
            secret: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(AgentPingResult.EndpointGone, result);
    }

    [Fact]
    public async Task PingAsync_Should_ReturnTimeout_When_ServerDoesNotRespond()
    {
        // arrange
        var client = CreateClient(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            TimeSpan.FromMilliseconds(100));

        // act
        var result = await client.PingAsync(
            "http://localhost:4096",
            "ses_123",
            secret: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Timeout, result);
    }

    private static OpencodeServerClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory,
        TimeSpan? timeout = null)
        => new(
            new HttpClient(new FakeOpencodeServer(responseFactory)),
            timeout ?? TimeSpan.FromSeconds(1));

    private sealed class FakeOpencodeServer(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responseFactory(request, cancellationToken);
    }
}
