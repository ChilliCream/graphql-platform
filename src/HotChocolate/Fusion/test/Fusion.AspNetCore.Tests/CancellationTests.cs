using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

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
