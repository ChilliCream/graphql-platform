using System.Runtime.CompilerServices;
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
    public async Task Null_Bubble_Cancellation_Does_Not_Poison_Subsequent_Request()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            b => b.AddQueryType<NullBubbleSchemaA.Query>());

        using var serverB = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullBubbleSchemaB.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA),
                ("B", serverB)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(5)));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var uri = new Uri("http://localhost:5000/graphql");

        // Warm up the gateway so the executor build and request-pipeline JIT happen
        // outside the loop below.
        using (await client.PostAsync(
            new OperationRequest("{ __typename }"),
            uri,
            TestContext.Current.CancellationToken))
        {
        }

        gateway.Interactions.Clear();

        // `c` always errors and null-bubbles to the root, cancelling the in-flight
        // sibling `b` (which is slow). The follow-up `{ a b }` must come back intact.
        var trigger = new OperationRequest("{ c b }");
        var normal = new OperationRequest("{ a b }");
        var poisonedRounds = new List<string>();

        // act
        for (var round = 0; round < 20; round++)
        {
            using (await client.PostAsync(trigger, uri, TestContext.Current.CancellationToken))
            {
            }

            using var normalResponse = await client.PostAsync(
                normal,
                uri,
                TestContext.Current.CancellationToken);
            using var result = await normalResponse.ReadAsResultAsync(
                TestContext.Current.CancellationToken);

            if (result.Errors.ValueKind == JsonValueKind.Array && result.Errors.GetArrayLength() > 0)
            {
                poisonedRounds.Add($"round {round}: unexpected errors");
                continue;
            }

            var a = result.Data.TryGetProperty("a", out var aValue) ? aValue.GetString() : null;
            var b = result.Data.TryGetProperty("b", out var bValue) ? bValue.GetString() : null;

            if (a != "A" || b != "B")
            {
                poisonedRounds.Add($"round {round}: a='{a}', b='{b}'");
            }
        }

        // assert
        Assert.True(
            poisonedRounds.Count == 0,
            $"The follow-up request was poisoned: {string.Join("; ", poisonedRounds)}.");
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
            public async Task<string> B(CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
                return "B";
            }
        }
    }

    private const int NullBubbleEventCount = 12;

    [Fact]
    public async Task Null_Bubble_In_One_Event_Does_Not_Poison_Next_Event()
    {
        // arrange
        using var streamServer = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<NullBubbleStream.Query>()
                .AddSubscriptionType<NullBubbleStream.Subscription>());

        using var titleServer = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullBubbleTitle.Query>());

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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

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

        // act
        // Each event fans out to a slow `title` fetch on B and a fast `trigger` fetch on C.
        // For even ids `trigger` (non-null) errors and null-bubbles to the root, cancelling
        // the still-in-flight B fetch. The following odd event must resolve `title` intact.
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            cts.Token);

        var results = new List<OperationResult>();
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            results.Add(result);

            if (results.Count == NullBubbleEventCount)
            {
                break;
            }
        }

        // assert
        // Events arrive in source order, so result[i] corresponds to id i + 1. Even ids are
        // expected to null-bubble; odd ids must carry their complete cross-subgraph data.
        var poisoned = new List<string>();

        for (var i = 0; i < results.Count; i++)
        {
            var id = i + 1;

            if (id % 2 == 0)
            {
                continue;
            }

            var result = results[i];

            if (result.Errors.ValueKind == JsonValueKind.Array && result.Errors.GetArrayLength() > 0)
            {
                poisoned.Add($"event {id}: unexpected errors");
                continue;
            }

            if (result.Data.ValueKind != JsonValueKind.Object
                || !result.Data.TryGetProperty("onBookCreated", out var book)
                || book.ValueKind != JsonValueKind.Object)
            {
                poisoned.Add($"event {id}: missing data");
                continue;
            }

            var title = book.GetProperty("title").GetString();
            var trigger = book.GetProperty("trigger").GetString();

            if (title != $"Title {id}" || trigger != "ok")
            {
                poisoned.Add($"event {id}: title='{title}', trigger='{trigger}'");
            }
        }

        foreach (var result in results)
        {
            result.Dispose();
        }

        Assert.True(
            results.Count == NullBubbleEventCount,
            $"Expected {NullBubbleEventCount} events but received {results.Count} "
            + "(a poisoned event can stall the stream).");
        Assert.True(
            poisoned.Count == 0,
            $"The follow-up event(s) were poisoned: {string.Join("; ", poisoned)}.");
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
                for (var id = 1; id <= NullBubbleEventCount; id++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
            public async Task<Book?> GetBookById(int id, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
                return new Book(id, $"Title {id}");
            }
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
                if (id % 2 == 0)
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
