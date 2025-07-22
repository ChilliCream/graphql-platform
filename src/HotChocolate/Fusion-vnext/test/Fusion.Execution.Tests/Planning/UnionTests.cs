namespace HotChocolate.Fusion.Planning;

// TODO once execution is implemented:
// - Returning null for a union (list item)
// TODO
// - spreading interface selection on union field
public class UnionTests : FusionTestBase
{
    #region union { ... }

    [Fact]
    public void Union_Field_Just_Typename_Selected()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              imageUrl: String!
            }

            type Discussion {
              id: ID!
              title: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              post {
                __typename
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              post {
                ... on Photo {
                  subgraph2
                }
                ... on Discussion {
                  subgraph1
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraph3 = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph3: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2, subgraph3);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph3
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_Field_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph2
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_Field_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  product {
                    subgraph2
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    #endregion

    #region unions { ... }

    [Fact]
    public void Union_List_Concrete_Type_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              posts {
                ... on Photo {
                  subgraph2
                }
                ... on Discussion {
                  subgraph1
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_List_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraph3 = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph3: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2, subgraph3);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph3
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_List_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph2
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_List_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  product {
                    subgraph2
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    #endregion

    #region objects { union { ... } }

    [Fact]
    public void Object_List_Union_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    subgraph2
                  }
                  ... on Discussion {
                    subgraph1
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Object_List_Union_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraph3 = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph3: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2, subgraph3);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph3
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Object_List_Union_Field_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Object_List_Union_Field_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    product {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    #endregion

    #region objects { unions { ... } }

        [Fact]
    public void Object_List_Union_List_Concrete_Type_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    subgraph2
                  }
                  ... on Discussion {
                    subgraph1
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Object_List_Union_List_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraph3 = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph3: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2, subgraph3);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph3
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Object_List_Union_List_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }

            type Author @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Object_List_Union_List_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        var subgraph1 = new TestSubgraph(
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraph2 = new TestSubgraph(
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              subgraph2: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraph1, subgraph2);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    product {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    #endregion
}
