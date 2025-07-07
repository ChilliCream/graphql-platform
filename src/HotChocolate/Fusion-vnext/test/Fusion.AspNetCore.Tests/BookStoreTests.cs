using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class BookStoreTests : FusionTestBase
{
    [Fact]
    public async Task Fetch_Book_From_SourceSchema1()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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
            {
              bookById(id: 1) {
                id
                title
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Book_From_SourceSchema1_And_Author_From_SourceSchema2()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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
            {
              bookById(id: 1) {
                title
                author {
                  name
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
        public record Book(int Id, string Title, Author Author);

        public record Author(int Id);

        public class Query
        {
            [Lookup]
            public Book GetBookById(int id)
                => new Book(id, "C# in Depth", new Author(1));
        }
    }

    public static class SourceSchema2
    {
        public record Author(int Id, string Name);

        public class Query
        {
            [Internal]
            [Lookup]
            public Author GetAuthorById(int id)
                => new Author(id, "Jon Skeet");
        }
    }
}
