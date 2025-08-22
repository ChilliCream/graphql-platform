using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

// TODO: Remove __typename for realistic results
// TODO: Fix abstract lookups
public class AbstractTypeTests : FusionTestBase
{
    [Fact]
    public async Task Abstract_Type()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>()
                .AddType<SourceSchema1.Discussion>()
                .AddType<SourceSchema1.Author>()
                .AddType<SourceSchema1.Product>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                abstractType {
                  __typename
                  id
                  ... on Discussion {
                    title
                  }
                  ... on Author {
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

    [Fact]
    public async Task Abstract_Type_With_Concrete_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>()
                .AddType<SourceSchema1.Discussion>()
                .AddType<SourceSchema1.Author>()
                .AddType<SourceSchema1.Product>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b
                .AddQueryType<SourceSchema2.Query>()
                .AddType<SourceSchema2.Author>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                abstractType {
                  __typename
                  id
                  ... on Discussion {
                    title
                    commentCount
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
    public async Task Abstract_Type_With_Abstract_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>()
                .AddType<SourceSchema1.Discussion>()
                .AddType<SourceSchema1.Author>()
                .AddType<SourceSchema1.Product>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b
                .AddQueryType<SourceSchema2.Query>()
                .AddType<SourceSchema2.Author>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                otherAbstractType {
                  __typename
                  id
                  ... on Author {
                    name
                    age
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
    public async Task Concrete_Type_With_Abstract_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>()
                .AddType<SourceSchema1.Discussion>()
                .AddType<SourceSchema1.Author>()
                .AddType<SourceSchema1.Product>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b
                .AddQueryType<SourceSchema2.Query>()
                .AddType<SourceSchema2.Author>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                authorById(id: 1) {
                  name
                  age
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task List_Of_Abstract_Types()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>()
                .AddType<SourceSchema1.Discussion>()
                .AddType<SourceSchema1.Author>()
                .AddType<SourceSchema1.Product>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                abstractTypes {
                  __typename
                  id
                  ... on Discussion {
                    title
                  }
                  ... on Author {
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
        public class Query
        {
            public SharedType GetAbstractType() => new Discussion(1);

            public SharedType GetOtherAbstractType() => new Author(1);

            public List<SharedType> GetAbstractTypes() => [new Discussion(1), new Author(2), new Product(3)];

            [Lookup]
            public Author GetAuthorById(int id) => new Author(id);
        }

        [InterfaceType]
        public interface SharedType
        {
            int Id { get; }
        }

        public record Discussion(int Id) : SharedType
        {
            public string Title { get; init; } = "Discussion " + Id;
        }

        public record Author(int Id) : SharedType
        {
            public string Name { get; init; } = "Author " + Id;
        }

        public record Product(int Id) : SharedType;
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Lookup]
            public OtherInterface GetOtherInterface(int id)
                => new Author(id, id * 5);

            [Lookup]
            [Internal]
            public Discussion? GetDiscussionById([ID] int id)
                => new Discussion(id, id * 3);
        }

        public record Discussion(int Id, int CommentCount);

        public interface OtherInterface
        {
            int Id { get; }
        }

        public record Author(int Id, int Age) : OtherInterface;
    }
}
