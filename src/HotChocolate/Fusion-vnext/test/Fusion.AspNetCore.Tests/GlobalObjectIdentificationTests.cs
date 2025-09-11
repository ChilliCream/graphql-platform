using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class GlobalObjectIdentificationTests : FusionTestBase
{
    [Fact]
    public async Task Concrete_Type_Branch_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
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

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Concrete_Type_Branch_Requested_Abstract_Lookup()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
             type Query {
               node(id: ID!): Node @lookup
               discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
             }

             interface Node {
               id: ID!
             }

             type Discussion implements Node {
               id: ID!
               title: String!
             }
             """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
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

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Invalid_Id_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
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

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Id_Of_Unknown_Type_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
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

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    // TODO: This should return a product
    public async Task Id_Of_Type_Different_From_Concrete_Type_Selections_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
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

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Only_TypeName_Selected_On_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
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

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task No_By_Id_Lookup_On_Best_Matching_Source_Schema()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              discussionByName(title: String! @is(field: "title")): Discussion @lookup
            }

            type Discussion {
              id: ID!
              title: String!
              commentCount: Int!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
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

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }
}
