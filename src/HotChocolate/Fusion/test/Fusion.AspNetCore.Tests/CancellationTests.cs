using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;
using OperationResult = HotChocolate.Transport.OperationResult;

namespace HotChocolate.Fusion;

public class CancellationTests : FusionTestBase
{
    [Fact]
    public async Task Request_Is_Running_Into_Execution_Timeout_While_Http_Request_In_Node_Is_Still_Ongoing()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>(),
            isTimingOut: true);

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(250)));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                topProduct {
                    id
                }
            }
            """);

        // Warm up the gateway so the executor build and request-pipeline JIT happen
        // outside the tight execution timeout below, leaving the 250ms budget to
        // measure the subgraph delay rather than first-request cold start. The
        // `__typename` meta-field resolves on the gateway and never reaches the
        // subgraph.
        using (await client.PostAsync(
            new OperationRequest("{ __typename }"),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken))
        {
        }

        // Discard any interactions recorded during warm-up so the snapshot reflects
        // only the measured request.
        gateway.Interactions.Clear();

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Http_Request_To_Source_Schema_Hits_HttpClient_Timeout()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>(),
            configureHttpClient: client => client.Timeout = TimeSpan.FromMilliseconds(250),
            isTimingOut: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                topProduct {
                    id
                }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Null_Bubble_Request_Waits_For_In_Flight_Sibling_Before_Completing()
    {
        // arrange
        // `c` on A null-bubbles to the root and cancels the request while `b` on B is
        // still in flight. The gating handler holds B's first HTTP call open (ignoring the
        // cancellation token) so the sibling fetch is genuinely unsettled at that point.
        var siblingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSibling = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new GatingHandler(siblingStarted, releaseSibling);

        using var serverA = CreateSourceSchema(
            "A",
            b => b.AddQueryType<NullBubbleSchemaA.Query>());

        using var serverB = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullBubbleSchemaB.Query>(),
            httpClient: new HttpClient(gate));
        gate.InnerHandler = serverB.CreateHandler();

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA),
                ("B", serverB)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(30)));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var uri = new Uri("http://localhost:5000/graphql");
        var ct = TestContext.Current.CancellationToken;

        // act
        var requestTask = client.PostAsync(new OperationRequest("{ c b }"), uri, ct);

        // B's sibling fetch is now in flight under the gate while `c` null-bubbles.
        await siblingStarted.Task;

        var completedBeforeRelease =
            await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(1), ct)) == requestTask;

        releaseSibling.SetResult();

        using var response = await requestTask;
        using var result = await response.ReadAsResultAsync(ct);

        // assert
        Assert.False(
            completedBeforeRelease,
            "The request returned before its in-flight sibling subgraph fetch settled.");
        Assert.Equal(JsonValueKind.Null, result.Data.ValueKind);
        Assert.True(
            result.Errors.ValueKind == JsonValueKind.Array && result.Errors.GetArrayLength() == 1,
            "Expected exactly one error from the null-bubbled `c` field.");
    }

    [Fact]
    public async Task Null_Bubble_Event_Waits_For_In_Flight_Sibling_Before_Emitting()
    {
        // arrange
        // The first event's `trigger` (on C) null-bubbles to the root while its `title`
        // fetch (on B) is held in flight by the gating handler, so the event must not be
        // emitted until the sibling settles. The next event must arrive intact.
        var siblingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSibling = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new GatingHandler(siblingStarted, releaseSibling);

        using var streamServer = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<NullBubbleStream.Query>()
                .AddSubscriptionType<NullBubbleStream.Subscription>());

        using var titleServer = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullBubbleTitle.Query>(),
            httpClient: new HttpClient(gate));
        gate.InnerHandler = titleServer.CreateHandler();

        using var triggerServer = CreateSourceSchema(
            "C",
            b => b.AddQueryType<NullBubbleTrigger.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", streamServer),
                ("B", titleServer),
                ("C", triggerServer)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(30)));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var ct = TestContext.Current.CancellationToken;

        var request = new OperationRequest(
            """
            subscription {
              onBookCreated {
                id
                title
                trigger
              }
            }
            """);

        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            ct);

        await using var enumerator =
            response.ReadAsResultStreamAsync().GetAsyncEnumerator(ct);

        // act
        var firstMove = enumerator.MoveNextAsync();
        var firstMoveTask = firstMove.AsTask();

        // Event 1's `title` fetch is now in flight under the gate while `trigger` null-bubbles.
        await siblingStarted.Task;

        var emittedBeforeRelease =
            await Task.WhenAny(firstMoveTask, Task.Delay(TimeSpan.FromSeconds(1), ct)) == firstMoveTask;

        releaseSibling.SetResult();

        // assert
        Assert.False(
            emittedBeforeRelease,
            "The subscription event was emitted before its in-flight sibling subgraph fetch settled.");

        Assert.True(await firstMoveTask, "Expected the first (null-bubbled) event to be emitted.");
        using (var firstEvent = enumerator.Current)
        {
            Assert.Equal(JsonValueKind.Null, firstEvent.Data.ValueKind);
        }

        Assert.True(await enumerator.MoveNextAsync(), "Expected a second event to be emitted.");
        using var secondEvent = enumerator.Current;
        var book = secondEvent.Data.GetProperty("onBookCreated");
        var title = book.GetProperty("title").GetString();
        var trigger = book.GetProperty("trigger").GetString();

        Assert.True(
            secondEvent.Errors.ValueKind != JsonValueKind.Array && title == "Title 2" && trigger == "ok",
            $"The follow-up event was poisoned: title='{title}', trigger='{trigger}'.");
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("application/jsonl")]
    public async Task Subscribe_Should_TerminateWithError_When_SourceSchemaStreamStalls(string mediaType)
    {
        // arrange
        // the source schema answers with one event, then keeps the connection open without sending more bytes
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<TickStream.Query>()
                .AddSubscriptionType<TickStream.Subscription>(),
            subscriptionReadTimeout: TimeSpan.FromMilliseconds(500),
            subscriptionAcceptHeaderValues: [new MediaTypeWithQualityHeaderValue(mediaType) { CharSet = "utf-8" }],
            mockHttpResponse: _ => Task.FromResult(CreateStalledStreamResponse(mediaType)));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var guard = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        guard.CancelAfter(TimeSpan.FromSeconds(10));

        var request = new OperationRequest("subscription { onTick }");

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            guard.Token);

        var results = await ReadUntilStreamEndsAsync(response, guard);

        // assert
        results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "onTick": 1
                  }
                }
                """,
                """
                {
                  "data": null,
                  "errors": [
                    {
                      "message": "Unexpected Execution Error",
                      "path": [
                        "onTick"
                      ]
                    }
                  ]
                }
                """
            ]);
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("application/jsonl")]
    public async Task Subscribe_Should_StayAlive_When_SourceSchemaOnlySendsKeepAlives(string mediaType)
    {
        // arrange
        // one event, then only keep-alive bytes for longer than the read timeout, then a second event, then a stall
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<TickStream.Query>()
                .AddSubscriptionType<TickStream.Subscription>(),
            subscriptionReadTimeout: TimeSpan.FromMilliseconds(500),
            subscriptionAcceptHeaderValues: [new MediaTypeWithQualityHeaderValue(mediaType) { CharSet = "utf-8" }],
            mockHttpResponse: _ => Task.FromResult(CreateKeepAliveStreamResponse(mediaType)));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var guard = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        guard.CancelAfter(TimeSpan.FromSeconds(10));

        var request = new OperationRequest("subscription { onTick }");

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            guard.Token);

        var results = await ReadUntilStreamEndsAsync(response, guard);

        // assert
        // both events arrive because keep-alive bytes reset the read deadline; only the final stall ends the stream
        results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "onTick": 1
                  }
                }
                """,
                """
                {
                  "data": {
                    "onTick": 2
                  }
                }
                """,
                """
                {
                  "data": null,
                  "errors": [
                    {
                      "message": "Unexpected Execution Error",
                      "path": [
                        "onTick"
                      ]
                    }
                  ]
                }
                """
            ]);
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("application/jsonl")]
    public async Task Subscribe_Should_CancelSourceSchemaStream_When_GatewayClientDisconnects(string mediaType)
    {
        // arrange
        // The source schema delivers one event and then idles; it signals when its stream is torn down.
        var signal = new SubscriptionTeardownSignal();

        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<IdleStream.Query>()
                .AddSubscriptionType<IdleStream.Subscription>(),
            configureServices: s => s.AddSingleton(signal),
            subscriptionAcceptHeaderValues: [new MediaTypeWithQualityHeaderValue(mediaType) { CharSet = "utf-8" }]);

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var guard = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        guard.CancelAfter(TimeSpan.FromSeconds(10));
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(guard.Token);

        var request = new OperationRequest("subscription { onTick }");

        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            requestCts.Token);

        var results = response.ReadAsResultStreamAsync().GetAsyncEnumerator(requestCts.Token);

        try
        {
            // receive one event while the connection is alive
            Assert.True(await results.MoveNextAsync(), "Expected the first event to be delivered.");

            // act
            // the subscription is idle; drop the gateway client connection while the next read is pending
            var next = results.MoveNextAsync().AsTask();
            await requestCts.CancelAsync();
            await IgnoreTeardownExceptionsAsync(next);
        }
        finally
        {
            await IgnoreTeardownExceptionsAsync(results.DisposeAsync().AsTask());
        }

        await Task.WhenAny(signal.TornDown.Task, Task.Delay(TimeSpan.FromSeconds(5), guard.Token));

        // assert
        Assert.True(
            signal.TornDown.Task.IsCompleted,
            "The source schema subscription stream was not torn down within 5 seconds "
            + "after the gateway client disconnected.");
        Assert.Equal($"{mediaType}; charset=utf-8", gateway.Interactions["A"].Values.Single().ContentType);
    }

    private sealed class GatingHandler : DelegatingHandler
    {
        private readonly TaskCompletionSource _started;
        private readonly TaskCompletionSource _release;
        private int _callCount;

        public GatingHandler(TaskCompletionSource started, TaskCompletionSource release)
        {
            _started = started;
            _release = release;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Hold only the first subgraph call open, ignoring the cancellation token, so the
            // gateway's node for it stays genuinely in flight after the null-bubble cancels.
            if (Interlocked.Increment(ref _callCount) == 1)
            {
                _started.TrySetResult();
                await _release.Task;

                // The execution token is cancelled by now; forward without it so the gated
                // response still comes back cleanly (its data is discarded by the null-bubble).
                return await base.SendAsync(request, CancellationToken.None);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

    private static HttpResponseMessage CreateStalledStreamResponse(string mediaType)
        => CreateScriptedStreamResponse(mediaType, [(TimeSpan.Zero, FormatTickEvent(mediaType, 1))]);

    private static HttpResponseMessage CreateKeepAliveStreamResponse(string mediaType)
    {
        // the same keep-alive bytes a HotChocolate source schema emits while a stream is idle
        var keepAlive = mediaType == "text/event-stream" ? ":\n\n" : " \n";
        var interval = TimeSpan.FromMilliseconds(100);
        var script = new List<(TimeSpan Delay, string Text)> { (TimeSpan.Zero, FormatTickEvent(mediaType, 1)) };

        for (var i = 0; i < 20; i++)
        {
            script.Add((interval, keepAlive));
        }

        script.Add((interval, FormatTickEvent(mediaType, 2)));

        return CreateScriptedStreamResponse(mediaType, script);
    }

    private static string FormatTickEvent(string mediaType, int tick)
        => mediaType == "text/event-stream"
            ? $"event: next\ndata: {{\"data\":{{\"onTick\":{tick}}}}}\n\n"
            : $"{{\"data\":{{\"onTick\":{tick}}}}}\n";

    private static HttpResponseMessage CreateScriptedStreamResponse(
        string mediaType,
        IReadOnlyList<(TimeSpan Delay, string Text)> script)
    {
        var chunks = new (TimeSpan Delay, byte[] Bytes)[script.Count];

        for (var i = 0; i < script.Count; i++)
        {
            chunks[i] = (script[i].Delay, Encoding.UTF8.GetBytes(script[i].Text));
        }

        var content = new StreamContent(new ScriptedStream(chunks));
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType) { CharSet = "utf-8" };

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static async Task<List<OperationResult>> ReadUntilStreamEndsAsync(
        GraphQLHttpResponse response,
        CancellationTokenSource guard)
    {
        var results = new List<OperationResult>();

        try
        {
            await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(guard.Token))
            {
                results.Add(result);
            }
        }
        catch (OperationCanceledException) when (guard.IsCancellationRequested)
        {
            Assert.Fail(
                "The gateway subscription stream did not end within 10 seconds after the source "
                + $"schema stream stalled. Results received before the guard fired: {results.Count}.");
        }

        return results;
    }

    private static async Task IgnoreTeardownExceptionsAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // expected: the streamed read was aborted by the client
        }
        catch (IOException)
        {
            // expected: aborting an in-flight streamed read can surface as an I/O failure
        }
    }

    // A read-only stream that delivers a scripted sequence of chunks, each one after its delay, and
    // then never completes another read until that read is cancelled or the stream is disposed.
    private sealed class ScriptedStream((TimeSpan Delay, byte[] Bytes)[] script) : Stream
    {
        private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _index;
        private int _offset;
        private bool _delayElapsed;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_index >= script.Length)
            {
                // stall until the read is cancelled or the stream is disposed
                await _disposed.Task.WaitAsync(cancellationToken);
                throw new ObjectDisposedException(nameof(ScriptedStream));
            }

            var (delay, bytes) = script[_index];

            if (!_delayElapsed)
            {
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }

                _delayElapsed = true;
            }

            // a zero-byte read only probes for available data and completes once the chunk is due
            var count = Math.Min(bytes.Length - _offset, buffer.Length);
            bytes.AsSpan(_offset, count).CopyTo(buffer.Span);
            _offset += count;

            if (_offset == bytes.Length)
            {
                _index++;
                _offset = 0;
                _delayElapsed = false;
            }

            return count;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException("Only asynchronous reads are supported.");

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            _disposed.TrySetResult();
            base.Dispose(disposing);
        }
    }

    public sealed class SubscriptionTeardownSignal
    {
        public TaskCompletionSource TornDown { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class NullBubbleSchemaA
    {
        public class Query
        {
            public string A() => "A";

            public string C(IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage("Could not resolve c")
                    .SetPath(context.Path)
                    .Build());
        }
    }

    public sealed class NullBubbleSchemaB
    {
        public class Query
        {
            public string B() => "B";
        }
    }

    public static class NullBubbleStream
    {
        [EntityKey("id")]
        public record Book(int Id);

        public class Query
        {
            public string Foo() => "Foo";
        }

        public class Subscription
        {
            public async IAsyncEnumerable<Book> OnBookCreatedStream(
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (var id = 1; id <= 2; id++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                    yield return new Book(id);
                }
            }

            [Subscribe(With = nameof(OnBookCreatedStream))]
            public Book OnBookCreated([EventMessage] Book book) => book;
        }
    }

    public static class NullBubbleTitle
    {
        public record Book(int Id, string Title);

        public class Query
        {
            [Internal, Lookup]
            public Book? GetBookById(int id) => new(id, $"Title {id}");
        }
    }

    public static class NullBubbleTrigger
    {
        public record Book(int Id, string Trigger);

        public class Query
        {
            [Internal, Lookup]
            public Book? GetBookById(int id, IResolverContext context)
            {
                if (id == 1)
                {
                    throw new GraphQLException(ErrorBuilder.New()
                        .SetMessage("Could not resolve trigger")
                        .SetPath(context.Path)
                        .Build());
                }

                return new Book(id, "ok");
            }
        }
    }

    public static class TickStream
    {
        public class Query
        {
            public string Foo() => "Foo";
        }

        public class Subscription
        {
            public async IAsyncEnumerable<int> OnTickStream()
            {
                yield return 1;
                await Task.CompletedTask;
            }

            [Subscribe(With = nameof(OnTickStream))]
            public int OnTick([EventMessage] int tick) => tick;
        }
    }

    public static class IdleStream
    {
        public class Query
        {
            public string Foo() => "Foo";
        }

        public class Subscription
        {
            public async IAsyncEnumerable<int> OnTickStream(
                [Service] SubscriptionTeardownSignal signal,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                try
                {
                    // deliver one event, then stay open and idle until the stream is torn down
                    yield return 1;
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                finally
                {
                    signal.TornDown.TrySetResult();
                }
            }

            [Subscribe(With = nameof(OnTickStream))]
            public int OnTick([EventMessage] int tick) => tick;
        }
    }

    public sealed class SourceSchema1
    {
        public class Query
        {
            public Product? TopProduct() => new(1);
        }

        public record Product(int Id);
    }

    public sealed class SourceSchema2
    {
        public class Query
        {
            public Review[]? Reviews(IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage("Could not resolve reviews")
                    .SetPath(context.Path)
                    .Build());
        }

        public class Subscription
        {
            public async IAsyncEnumerable<Review> OnReviewCreatedStream()
            {
                yield return new Review(1);

                await Task.Delay(250);

                yield return new Review(2);
            }

            [Subscribe(With = nameof(OnReviewCreatedStream))]
            public Review? OnReviewCreated([EventMessage] Review review, IResolverContext context)
            {
                if (review.Id == 2)
                {
                    throw new GraphQLException(ErrorBuilder.New()
                        .SetMessage("Could not produce review")
                        .SetPath(context.Path)
                        .Build());
                }

                return review;
            }
        }

        public record Review(int Id);
    }
}
