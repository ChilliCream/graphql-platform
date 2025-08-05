using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SubscriptionsOverHttpStoreTests : FusionTestBase
{
    [Fact]
    public async Task Subscribe_Simple()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>()
                .AddSubscriptionType<SourceSchema1.Subscription>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            subscription {
              onBookCreated {
                id
                title
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        var snapshot = new Snapshot();

        await foreach (var response in result.ReadAsResultStreamAsync())
        {
            snapshot.Add(response);
        }

        await snapshot.MatchAsync();
    }

    public static class SourceSchema1
    {
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

                await Task.Delay(200);
                yield return new Book(2);

                await Task.Delay(200);
                yield return new Book(3);
            }

            [Subscribe(With = nameof(OnBookCreatedStream))]
            public Book OnBookCreated([EventMessage] Book book)
                => book;
        }
    }

    public static class SourceSchema2
    {
        public record Book(int Id, string Title);

        public class Query
        {
            [Internal, Lookup]
            public Book GetBookById(int id)
            {
                switch (id)
                {
                    case 1:
                        return new Book(1, "Foo");

                    case 2:
                        return new Book(2, "Bar");

                    case 3:
                        return new Book(3, "Baz");

                    default:
                        throw new ArgumentOutOfRangeException(nameof(id));
                }
            }
        }
    }
}
