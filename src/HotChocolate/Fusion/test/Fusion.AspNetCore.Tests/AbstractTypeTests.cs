using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

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

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              abstractType {
                id
                ... on Discussion {
                  title
                }
                ... on Author {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // act
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Abstract_Type_Direct_Source_Schema_Call()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>()
                .AddType<SourceSchema1.Discussion>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              interfaceConnection(first: 2) {
                edges {
                  node {
                    id
                  }
                }
                pageInfo {
                  hasNextPage
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
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

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                abstractType {
                  id
                  ... on Discussion {
                    title
                    commentCount
                  }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
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

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                otherAbstractType {
                  id
                  ... on Author {
                    name
                    age
                  }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
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

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                authorById(id: 1) {
                  name
                  age
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
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

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                abstractTypes {
                  id
                  ... on Discussion {
                    title
                  }
                  ... on Author {
                    name
                  }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Interface_Field_Without_Type_Refinements_With_Interface_Lookup()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
            }

            interface Votable @key(fields: "id") {
              id: ID!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              votableById(id: ID!): Votable @lookup
            }

            interface Votable {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              votable {
                id
                viewerCanVote
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Interface_Field_Without_Type_Refinements_With_Concrete_Lookups()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
            }

            interface Votable @key(fields: "id") {
              id: ID!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup
              commentById(id: ID!): Comment @lookup
            }

            interface Votable {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              votable {
                id
                viewerCanVote
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Interface_Field_Without_Type_Refinements_With_Interface_Lookup_And_Field_From_Specific_Source()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
            }

            interface Votable @key(fields: "id") {
              id: ID!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              votableById(id: ID!): Votable @lookup @shareable
            }

            interface Votable {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var serverC = CreateSourceSchema(
            "C",
            """
            type Query {
              votableById(id: ID!): Votable @lookup @shareable
            }

            interface Votable {
              id: ID!
              totalVotes: Int!
            }

            type Discussion implements Votable {
              id: ID!
              totalVotes: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB),
            ("C", serverC)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              votable {
                id
                totalVotes
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Interface_Field_Without_Type_Refinements_With_Concrete_Lookups_And_Field_From_Specific_Source()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
            }

            interface Votable @key(fields: "id") {
              id: ID!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup @shareable
              commentById(id: ID!): Comment @lookup @shareable
            }

            interface Votable {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var serverC = CreateSourceSchema(
            "C",
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup @shareable
              commentById(id: ID!): Comment @lookup @shareable
            }

            interface Votable {
              id: ID!
              totalVotes: Int!
            }

            type Discussion implements Votable {
              id: ID!
              totalVotes: Int!
            }

            type Comment implements Votable {
              id: ID!
              totalVotes: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB),
            ("C", serverC)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              votable {
                id
                totalVotes
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_With_Type_Refinements_And_Concrete_Lookups()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              search: SearchResult
            }

            union SearchResult = User | Product

            type User @key(fields: "id") {
              id: ID!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              userById(id: ID!): User @lookup @shareable
              productById(id: ID!): Product @lookup
            }

            type User {
              id: ID!
              reputation: Int!
            }

            type Product {
              id: ID!
              price: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              search {
                ... on User {
                  reputation
                }
                ... on Product {
                  price
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_With_Type_Refinements_And_Union_Lookup()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              search: SearchResult
            }

            union SearchResult = User | Product

            type User @key(fields: "id") {
              id: ID!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              searchResultById(id: ID!): SearchResult @lookup
            }

            union SearchResult = User | Product

            type User {
              id: ID!
              reputation: Int!
            }

            type Product {
              id: ID!
              price: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              search {
                ... on User {
                  reputation
                }
                ... on Product {
                  price
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_With_Type_Refinements_And_Concrete_Lookups_With_Additional_Concrete_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              search: SearchResult
            }

            union SearchResult = User | Product

            type User @key(fields: "id") {
              id: ID!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              userById(id: ID!): User @lookup @shareable
              productById(id: ID!): Product @lookup
            }

            type User {
              id: ID!
              reputation: Int!
            }

            type Product {
              id: ID!
              price: Float!
            }
            """);

        using var serverC = CreateSourceSchema(
            "C",
            """
            type Query {
              userById(id: ID!): User @lookup @shareable
            }

            type User {
              id: ID!
              profile: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB),
            ("C", serverC)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              search {
                ... on User {
                  reputation
                  profile
                }
                ... on Product {
                  price
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_With_Type_Refinements_And_Union_Lookup_With_Additional_Concrete_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              search: SearchResult
            }

            union SearchResult = User | Product

            type User @key(fields: "id") {
              id: ID!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              searchResultById(id: ID!): SearchResult @lookup
            }

            union SearchResult = User | Product

            type User {
              id: ID!
              reputation: Int!
            }

            type Product {
              id: ID!
              price: Float!
            }
            """);

        using var serverC = CreateSourceSchema(
            "C",
            """
            type Query {
              userById(id: ID!): User @lookup
            }

            type User {
              id: ID!
              profile: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB),
            ("C", serverC)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              search {
                ... on User {
                  reputation
                  profile
                }
                ... on Product {
                  price
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public SharedType GetAbstractType() => new Discussion(1);

            public SharedType GetOtherAbstractType() => new Author(1);

            public List<SharedType> GetAbstractTypes() => [new Discussion(1), new Author(2), new Product(3)];

            [UsePaging]
            public IEnumerable<SharedType> InterfaceConnection()
                => [new Discussion(1)];

            [Lookup]
            public Author GetAuthorById(int id) => new Author(id);
        }

        [InterfaceType]
        [EntityKey("id")]
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
