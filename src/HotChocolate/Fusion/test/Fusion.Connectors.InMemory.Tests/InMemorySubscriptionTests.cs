using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class InMemorySubscriptionTests
{
    [Fact]
    public async Task Subscribe_Should_DeliverEachEventWithCrossSchemaData_When_MultipleEvents()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddGraphQL("books")
            .AddQueryType<BooksSchema.Query>()
            .AddSubscriptionType<BooksSchema.Subscription>()
            .AddSourceSchemaDefaults();

        services.AddGraphQL("titles")
            .AddQueryType<TitlesSchema.Query>()
            .AddSourceSchemaDefaults();

        services.AddGraphQLGateway()
            .AddInMemorySchema("books")
            .AddInMemorySchema("titles");

        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);

        // act
        // Each event must be delivered in order and each event document must be usable while its
        // own cross-schema lookup runs, which only holds if events do not share one arena.
        var events = await CollectEventsAsync(executor, TestContext.Current.CancellationToken);

        // assert
        string.Join("\n---\n", events).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subscribe_Should_DeliverAllEvents_When_StreamIsConsumedToCompletion()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddGraphQL("books")
            .AddQueryType<BooksSchema.Query>()
            .AddSubscriptionType<BooksSchema.Subscription>()
            .AddSourceSchemaDefaults();

        services.AddGraphQL("titles")
            .AddQueryType<TitlesSchema.Query>()
            .AddSourceSchemaDefaults();

        services.AddGraphQLGateway()
            .AddInMemorySchema("books")
            .AddInMemorySchema("titles");

        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);

        // act
        // Consuming the stream to completion exercises the per-event arena swap and the teardown
        // path that releases every in-flight arena once the subscription ends.
        var events = await CollectEventsAsync(executor, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(3, events.Count);
    }

    private static async Task<List<string>> CollectEventsAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                subscription {
                  onBookCreated {
                    id
                    title
                  }
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request, cancellationToken);
        var stream = result.ExpectResponseStream();

        var events = new List<string>();

        await foreach (var operationResult in stream
            .ReadResultsAsync()
            .WithCancellation(cancellationToken))
        {
            events.Add(operationResult.ToJson());
        }

        await result.DisposeAsync();
        return events;
    }

    private static class BooksSchema
    {
        [EntityKey("id")]
        public record Book(int Id);

        public class Query
        {
            public string Foo() => "Foo";
        }

        public class Subscription
        {
            public async IAsyncEnumerable<Book> OnBookCreatedStream()
            {
                yield return new Book(1);

                await Task.Yield();
                yield return new Book(2);

                await Task.Yield();
                yield return new Book(3);
            }

            [Subscribe(With = nameof(OnBookCreatedStream))]
            public Book OnBookCreated([EventMessage] Book book)
                => book;
        }
    }

    private static class TitlesSchema
    {
        public record Book(int Id, string Title);

        public class Query
        {
            [Internal, Lookup]
            public Book? GetBookById(int id)
                => id switch
                {
                    1 => new Book(1, "Foo"),
                    2 => new Book(2, "Bar"),
                    3 => new Book(3, "Baz"),
                    _ => null
                };
        }
    }
}
