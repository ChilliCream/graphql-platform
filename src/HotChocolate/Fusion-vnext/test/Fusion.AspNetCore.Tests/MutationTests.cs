using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class MutationTests : FusionTestBase
{
    [Fact]
    public async Task Single_Mutation()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddMutationType<SourceSchema1.Mutation>());

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
            mutation {
              a: createBook(input: { title: "Book1" }) {
                book {
                  id
                  author
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Multiple_Mutation()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddMutationType<SourceSchema1.Mutation>());

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
            mutation {
              a: createBook(input: { title: "Book1" }) {
                book {
                  id
                  author
                }
              }
              b: createBook(input: { title: "Book2" }) {
                book {
                  id
                  title
                  author
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public string Foo => "Foo";
        }

        public class Mutation
        {
            private int _nextId;

            public CreateBookPayload CreateBook(CreateBookInput input)
            {
                var id = Interlocked.Increment(ref _nextId);
                return new CreateBookPayload(new Book(id, input.Title));
            }
        }

        public record CreateBookInput(string Title);

        public record CreateBookPayload(Book Book);

        public record Book(int Id, string Title);
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Internal, Lookup]
            public Book GetBookById(int id) => new(id, "Abc");
        }

        public record Book(int Id, string Author);
    }
}
