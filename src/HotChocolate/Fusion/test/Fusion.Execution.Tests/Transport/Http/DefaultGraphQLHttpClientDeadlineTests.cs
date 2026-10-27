using System.Net;
using System.Text;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Transport.Http;

public sealed class DefaultGraphQLHttpClientDeadlineTests
{
    private static readonly Uri s_requestUri = new("http://localhost:5000/graphql");

    [Fact]
    public async Task SendAsync_Should_PassCallerTokenToHandler_When_RequestHasDeadline()
    {
        // arrange
        var handler = new RecordingHandler();
        using var clientWithHandler = new DefaultGraphQLHttpClient(
            new HttpClient(handler, disposeHandler: false),
            handler);
        using var clientWithoutHandler = new DefaultGraphQLHttpClient(
            new HttpClient(handler, disposeHandler: false));
        using var cts = new CancellationTokenSource();
        var cases = new[]
        {
            (Name: "client with handler, deadline", Client: clientWithHandler, HasDeadline: true),
            (Name: "client with handler, no deadline", Client: clientWithHandler, HasDeadline: false),
            (Name: "client without handler, deadline", Client: clientWithoutHandler, HasDeadline: true)
        };
        var results = new List<string>();

        // act
        foreach (var (name, client, hasDeadline) in cases)
        {
            using var response = await client.SendAsync(CreateRequest(hasDeadline), cts.Token);
            var body = await response.HttpResponseMessage.Content.ReadAsStringAsync(cts.Token);

            results.Add(
                $"{name}: callerToken={handler.Token == cts.Token}, version={handler.Version}, "
                + $"policy={handler.VersionPolicy}, uri={handler.RequestUri}, body={body}");
        }

        // assert
        results.MatchInlineSnapshots(
        [
            """client with handler, deadline: callerToken=True, version=1.1, policy=RequestVersionOrLower, uri=http://localhost:5000/graphql, body={"data":{"number":1}}""",
            """client with handler, no deadline: callerToken=False, version=1.1, policy=RequestVersionOrLower, uri=http://localhost:5000/graphql, body={"data":{"number":1}}""",
            """client without handler, deadline: callerToken=False, version=1.1, policy=RequestVersionOrLower, uri=http://localhost:5000/graphql, body={"data":{"number":1}}"""
        ]);
    }

    [Fact]
    public async Task SendAsync_Should_IgnoreHttpClientTimeout_When_RequestHasDeadline()
    {
        // arrange
        // the handler answers after the HTTP client timeout has elapsed
        var handler = new RecordingHandler(TimeSpan.FromMilliseconds(300));
        var httpClient = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromMilliseconds(50) };
        using var client = new DefaultGraphQLHttpClient(httpClient, handler);
        var results = new List<string>();

        // act
        foreach (var hasDeadline in new[] { true, false })
        {
            try
            {
                using var response = await client.SendAsync(
                    CreateRequest(hasDeadline),
                    TestContext.Current.CancellationToken);
                results.Add($"deadline={hasDeadline}: {response.StatusCode}");
            }
            catch (OperationCanceledException ex)
            {
                results.Add($"deadline={hasDeadline}: {ex.GetType().Name} ({ex.InnerException?.GetType().Name})");
            }
        }

        // assert
        results.MatchInlineSnapshots(
        [
            "deadline=True: OK",
            "deadline=False: TaskCanceledException (TimeoutException)"
        ]);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowWithCallerToken_When_CallerCancels()
    {
        // arrange
        var results = new List<string>();

        // act
        foreach (var hasDeadline in new[] { true, false })
        {
            var handler = new RecordingHandler(Timeout.InfiniteTimeSpan);
            using var client = new DefaultGraphQLHttpClient(new HttpClient(handler, disposeHandler: false), handler);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var sendTask = client.SendAsync(CreateRequest(hasDeadline), cts.Token);
            await handler.WaitForRequestAsync();
            await cts.CancelAsync();

            try
            {
                using var response = await sendTask;
                results.Add($"deadline={hasDeadline}: {response.StatusCode}");
            }
            catch (OperationCanceledException ex)
            {
                results.Add($"deadline={hasDeadline}: {ex.GetType().Name}, callerToken={ex.CancellationToken == cts.Token}");
            }
        }

        // assert
        results.MatchInlineSnapshots(
        [
            "deadline=True: TaskCanceledException, callerToken=True",
            "deadline=False: TaskCanceledException, callerToken=True"
        ]);
    }

    [Fact]
    public async Task SendAsync_Should_InvokeMessageCallbacks_When_RequestHasDeadline()
    {
        // arrange
        var handler = new RecordingHandler();
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler, disposeHandler: false), handler);
        HttpStatusCode? receivedStatusCode = null;
        var request = CreateRequest(hasDeadline: true);
        request.State = new RequestCallbackState();
        request.OnMessageCreated = static (_, message, _) => message.Headers.Add("X-Callback", "created");
        request.OnMessageReceived = (_, message, _) => receivedStatusCode = message.StatusCode;

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("created", handler.CallbackHeader);
        Assert.Equal(HttpStatusCode.OK, receivedStatusCode);
    }

    private static GraphQLHttpRequest CreateRequest(bool hasDeadline)
    {
        var operation = new OperationRequest(
            "{ number }"u8.ToArray(),
            id: null,
            operationName: null,
            onError: null,
            variables: VariableValues.Empty,
            extensions: JsonSegment.Empty);

        return new GraphQLHttpRequest(operation, s_requestUri) { HasDeadline = hasDeadline };
    }

    private sealed class RecordingHandler(TimeSpan? delay = null) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _requestReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken Token { get; private set; }

        public Version? Version { get; private set; }

        public HttpVersionPolicy? VersionPolicy { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? CallbackHeader { get; private set; }

        public Task WaitForRequestAsync() => _requestReceived.Task;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            Version = request.Version;
            VersionPolicy = request.VersionPolicy;
            RequestUri = request.RequestUri;
            CallbackHeader = request.Headers.TryGetValues("X-Callback", out var values)
                ? string.Join(",", values)
                : null;
            _requestReceived.TrySetResult();

            if (delay is { } wait)
            {
                await Task.Delay(wait, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":{"number":1}}""",
                    Encoding.UTF8,
                    "application/graphql-response+json")
            };
        }
    }
}
