using System.Collections.Concurrent;
using System.Net;
using System.Text;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Transport.Http;

public class DefaultGraphQLHttpClientAsyncTests
{
    private static readonly Uri s_address = new("http://localhost:5000/graphql");
    private readonly AsyncLocal<string?> _correlation = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_PreserveBodyAndContext_When_RequestsOverlap(bool publicRoute)
    {
        // arrange
        var observed = new ConcurrentQueue<string>();
        using var handler = new AsyncHandler(async (request, cancellationToken) =>
        {
            var before = _correlation.Value;
            await Task.Yield();
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            observed.Enqueue($"{before}:{_correlation.Value}:{request.Version}:{body}");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new TrackingContent()
            };
        });
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        // act
        var completed = new List<string>();
        for (var wave = 0; wave < 3; wave++)
        {
            var tasks = Enumerable.Range(wave * 16, 16).Select(ExecuteAsync).ToArray();
            completed.AddRange(await Task.WhenAll(tasks));
        }

        // assert
        Assert.Equal(Enumerable.Range(0, 48).Select(i => $"{i}:True"), completed);
        Assert.Equal(
            Enumerable.Range(0, 48).Select(i => $"{i}:{i}:1.1:{{\"query\":\"{{ field{i} }}\"}}").Order(),
            observed.Order());

        async Task<string> ExecuteAsync(int index)
        {
            _correlation.Value = index.ToString();
            var request = CreateRequest($"{{ field{index} }}");
            var content = default(TrackingContent);
            using (var response = await SendAsync(client, request, publicRoute, TestContext.Current.CancellationToken))
            {
                content = (TrackingContent)response.HttpResponseMessage.Content;
            }
            return $"{_correlation.Value}:{content.Disposed}";
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_OrderCallbacksAndDisposeRequest_When_SendCompletes(bool publicRoute)
    {
        // arrange
        var events = new List<string>();
        HttpRequestMessage? message = null;
        _correlation.Value = "request";
        using var handler = new AsyncHandler(async (_, _) =>
        {
            events.Add($"sending:{_correlation.Value}");
            await Task.Yield();
            events.Add($"sent:{_correlation.Value}");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));
        var request = CreateRequest("{ field }");
        request.State = default(RequestCallbackState);
        request.OnMessageCreated = (_, created, _) =>
        {
            message = created;
            events.Add($"created:{_correlation.Value}");
        };
        request.OnMessageReceived = (_, _, _) => events.Add($"received:{_correlation.Value}");

        // act
        using var response = await SendAsync(client, request, publicRoute, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new[] { "created:request", "sending:request", "sent:request", "received:request" }, events);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => message!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_PropagateCancellation_When_SendIsSuspended(bool publicRoute)
    {
        // arrange
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var handler = new AsyncHandler(async (_, cancellationToken) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        // act
        var pending = SendAsync(client, CreateRequest("{ field }"), publicRoute, cancellation.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var suspended = !pending.IsCompleted;
        await cancellation.CancelAsync();
        var exception = await Record.ExceptionAsync(async () => await pending);

        // assert
        Assert.True(suspended);
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_DisposeRequest_When_TransportFails(bool publicRoute)
    {
        // arrange
        var failure = new HttpRequestException("transport failed");
        HttpRequestMessage? message = null;
        using var handler = new AsyncHandler(async (request, _) =>
        {
            message = request;
            await Task.Yield();
            throw failure;
        });
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        // act
        var pending = SendAsync(client, CreateRequest("{ field }"), publicRoute, TestContext.Current.CancellationToken);
        var exception = await Record.ExceptionAsync(async () => await pending);

        // assert
        Assert.Same(failure, exception);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => message!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SendAsync_Should_ReturnCallbackFailure_When_CallbackThrows(bool publicRoute, bool received)
    {
        // arrange
        var failure = new InvalidOperationException("callback failed");
        using var content = new TrackingContent();
        using var handler = new AsyncHandler(async (_, _) =>
        {
            await Task.Yield();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));
        var request = CreateRequest("{ field }");
        request.State = default(RequestCallbackState);
        if (received)
        {
            request.OnMessageReceived = (_, _, _) => throw failure;
        }
        else
        {
            request.OnMessageCreated = (_, _, _) => throw failure;
        }

        // act
        var pending = default(ValueTask<GraphQLHttpResponse>);
        var synchronous = Record.Exception(() => pending = SendAsync(client, request, publicRoute, TestContext.Current.CancellationToken));
        var asynchronous = await Record.ExceptionAsync(async () => await pending);

        // assert
        Assert.Null(synchronous);
        Assert.Same(failure, asynchronous);
        Assert.False(content.Disposed);
    }

    [Fact]
    public async Task SendAsync_Should_AllowRepeatedAwait_When_PublicTaskIsShared()
    {
        // arrange
        using var handler = new AsyncHandler(async (_, _) =>
        {
            await Task.Yield();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        // act
        var pending = client.SendAsync(CreateRequest("{ field }"), TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(pending, pending);
        using var response = await pending;

        // assert
        Assert.Same(response, results[0]);
        Assert.Same(response, results[1]);
    }

    private static GraphQLHttpRequest CreateRequest(string query)
        => new(new OperationRequest(Encoding.UTF8.GetBytes(query), null, null, null,
            VariableValues.Empty, JsonSegment.Empty), s_address);

    private static ValueTask<GraphQLHttpResponse> SendAsync(
        DefaultGraphQLHttpClient client,
        GraphQLHttpRequest request,
        bool publicRoute,
        CancellationToken cancellationToken)
        => publicRoute
            ? new ValueTask<GraphQLHttpResponse>(client.SendAsync(request, cancellationToken))
            : client.SendAsync(request, request.Uri!, cancellationToken);

    private sealed class AsyncHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }

    private sealed class TrackingContent() : StringContent("{\"data\":{\"field\":\"value\"}}", Encoding.UTF8, "application/json")
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
