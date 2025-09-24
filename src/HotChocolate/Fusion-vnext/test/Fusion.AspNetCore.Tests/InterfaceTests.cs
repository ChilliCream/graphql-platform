using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class InterfaceTests : FusionTestBase
{
    # region interface { ... }

    [Fact]
    public async Task Interface_Field()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votable {
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
    public async Task Interface_Field_Linked_Field_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              authorable {
                author {
                  id
                  displayName
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
    public async Task Interface_Field_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              authorable {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    id
                    displayName
                  }
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
    public async Task Interface_Field_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
              email: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              authorable {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    email
                  }
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
    public async Task Interface_Field_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votable {
                viewerCanVote
                ... on Discussion {
                  title
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
    public async Task Interface_Field_Concrete_Type_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
              discussionById(id: ID!): Discussion @lookup @shareable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup @shareable
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votable {
                viewerCanVote
                ... on Discussion {
                  viewerRating
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
    public async Task Interface_Field_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              author: Author
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votable {
                viewerCanVote
                ... on Discussion {
                  author {
                    displayName
                  }
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

    #endregion

    # region interfaces { ... }

    [Fact]
    public async Task Interface_List_Field()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votables {
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
    public async Task Interface_List_Field_Linked_Field_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              authorables {
                author {
                  id
                  displayName
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
    public async Task Interface_List_Field_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              authorables {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    id
                    displayName
                  }
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
    public async Task Interface_List_Field_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
              email: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              authorables {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    email
                  }
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
    public async Task Interface_List_Field_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votables {
                viewerCanVote
                ... on Discussion {
                  title
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
    public async Task Interface_List_Field_Concrete_Type_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votables: [Votable]
              discussionById(id: ID!): Discussion @lookup @shareable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup @shareable
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votables {
                viewerCanVote
                ... on Discussion {
                  viewerRating
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
    public async Task Interface_List_Field_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              author: Author
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              votables {
                viewerCanVote
                ... on Discussion {
                  author {
                    displayName
                  }
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

    #endregion

    #region wrappers { interface { ... } }

    [Fact]
    public async Task List_Field_Interface_Object_Property_Linked_Field_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              wrappers {
                authorable {
                  author {
                    displayName
                  }
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
    public async Task
        List_Field_Interface_Object_Property_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              wrappers {
                authorable {
                  author {
                    displayName
                  }
                  ... on Discussion {
                    author {
                      displayName
                    }
                  }
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
    public async Task
        List_Field_Interface_Object_Property_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
              email: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              wrappers {
                authorable {
                  author {
                    displayName
                  }
                  ... on Discussion {
                    author {
                      email
                    }
                  }
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
    public async Task List_Field_Interface_Object_Property_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              wrappers {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    title
                  }
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
    public async Task List_Field_Interface_Object_Property_Concrete_Type_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              wrappers: [Wrapper]
              discussionById(id: ID!): Discussion @lookup @shareable
            }

            type Wrapper {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup @shareable
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              wrappers {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    viewerRating
                  }
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
    public async Task List_Field_Interface_Object_Property_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              author: Author
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            query testQuery {
              wrappers {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    author {
                      displayName
                    }
                  }
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

    #endregion
}
