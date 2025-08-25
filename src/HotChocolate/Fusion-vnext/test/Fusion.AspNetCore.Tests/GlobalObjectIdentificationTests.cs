using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class GlobalObjectIdentificationTests : FusionTestBase
{
    [Fact]
    public async Task Concrete_Type_Branch_Requested()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema2.Query>());

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
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
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
    public async Task Concrete_Type_Branch_Requested_Abstract_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema2.Query>()
                .AddType<SourceSchema2.Product>());

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
              # Product:1
              node(id: "UHJvZHVjdDox") {
                id
                ... on Product {
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
    public async Task Invalid_Id_Requested()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema2.Query>());

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
              node(id: "invalid") {
                ... on Discussion {
                  title
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
    public async Task Id_Of_Unknown_Type_Requested()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema2.Query>());

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
              # User:1
              node(id: "VXNlcjox") {
                ... on Discussion {
                  title
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
    public async Task Id_Of_Type_Different_From_Concrete_Type_Selections_Requested()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema2.Query>()
                .AddType<SourceSchema2.Product>());

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
              # Product:1
              node(id: "UHJvZHVjdDox") {
                id
                ... on Discussion {
                  title
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
    public async Task Only_TypeName_Selected_On_Concrete_Type()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema2.Query>());

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
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Discussion {
                  __typename
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
    public async Task No_By_Id_Lookup_On_Best_Matching_Source_Schema()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema3.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .AddQueryType<SourceSchema1.Query>());

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
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Discussion {
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

    public static class SourceSchema1
    {
        public class Query
        {
            [Lookup]
            public Discussion? GetDiscussionById([Is("id")] [ID] int discussionId)
                => new Discussion(discussionId, "Discussion " + discussionId);
        }

        [Node]
        public record Discussion(int Id, string Title)
        {
            [NodeResolver]
            public static Discussion Get(int id)
                => new Discussion(id, "Discussion " + id);
        }
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Discussion? GetDiscussionById([ID] int id)
                => new Discussion(id, id * 3);
        }

        [Node]
        public record Discussion(int Id, int CommentCount)
        {
            [NodeResolver]
            public static Discussion Get(int id)
                => new Discussion(id, id * 3);
        }

        [Node]
        public record Product(int Id)
        {
            [NodeResolver]
            public static Product Get(int id)
                => new Product(id);

            public string Name => "Product " + Id;
        }
    }

    public static class SourceSchema3
    {
        public class Query
        {
            [Lookup]
            public Discussion? GetDiscussionByName([Is("title")] string title)
            {
                var id = int.Parse(title["Discussion ".Length..]);

                return new Discussion(id, title, id * 3);
            }
        }

        public record Discussion([property: ID] int Id, string Title, int CommentCount);
    }
}
