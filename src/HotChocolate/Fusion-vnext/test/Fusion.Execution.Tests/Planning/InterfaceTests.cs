namespace HotChocolate.Fusion.Planning;

public class InterfaceTests : FusionTestBase
{
    [Fact]
    public void Selections_On_Interface_Field()
    {
        // arrange
        var subgraphA = new TestSubgraph(
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

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votable {
                viewerCanVote
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  votable {
                    viewerCanVote
                  }
                }
            """);
    }

    [Fact]
    public void Selections_On_Interface_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
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

            type Author {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
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

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  authorable {
                    author {
                      id
                    }
                  }
                }
            - id: 2
              schema: SUBGRAPH_2
              operation: >-
                query testQuery_2 {
                  authorById(id: $__fusion_1_id) {
                    displayName
                  }
                }
              requirements:
                - name: __fusion_1_id
                  selectionSet: author.authorable
                  selectionMap: id
              dependencies:
                - id: 1
            """);
    }

    [Fact]
    public void Selections_On_Interface_Field_And_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
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
              title: String!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
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

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  votable {
                    viewerCanVote
                    ... on Discussion {
                      title
                    }
                  }
                }
            """);
    }

    [Fact]
    public void Selections_On_Interface_Field_And_Concrete_Type_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              title: String!
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorable {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  title
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  authorable {
                    author {
                      id
                    }
                    ... on Discussion {
                      title
                    }
                  }
                }
            - id: 2
              schema: SUBGRAPH_2
              operation: >-
                query testQuery_2 {
                  authorById(id: $__fusion_1_id) {
                    displayName
                  }
                }
              requirements:
                - name: __fusion_1_id
                  selectionSet: author.authorable
                  selectionMap: id
              dependencies:
                - id: 1
            """);
    }

    [Fact]
    public void Selections_On_Interface_List_Field()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votables: [Votable]
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

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votables {
                viewerCanVote
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  votables {
                    viewerCanVote
                  }
                }
            """);
    }

    [Fact(Skip = "Not yet supported")]
    public void Selections_On_Interface_List_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
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

            type Author {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorsById(ids: [ID!]!): [Author]!
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
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

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  authorables {
                    author {
                      id
                    }
                  }
                }
            """);
    }

    [Fact]
    public void Selections_On_Interface_List_Field_And_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
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

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  votables {
                    viewerCanVote
                    ... on Discussion {
                      title
                    }
                  }
                }
            """);
    }

    [Fact(Skip = "Not yet supported")]
    public void Selections_On_Interface_List_Field_And_Concrete_Type_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              title: String!
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorsById(ids: [ID!]!): [Author]
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorables {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  title
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  authorables {
                    author {
                      id
                    }
                    ... on Discussion {
                      title
                    }
                  }
                }
            """);
    }

    [Fact]
    public void Selections_On_Interface_On_Node_Field()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Votable {
                  viewerCanVote
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  node(id: $id) {
                    ... on Votable {
                      viewerCanVote
                    }
                  }
                }
            """);
    }

    [Fact]
    public void Selections_On_Interface_And_Concrete_Type_On_Node_Field()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Votable {
                  viewerCanVote
                }
                ... on Discussion {
                  viewerRating
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: SUBGRAPH_1
              operation: >-
                query testQuery_1 {
                  node(id: $id) {
                    ... on Votable {
                      viewerCanVote
                    }
                    ... on Discussion {
                      viewerRating
                    }
                  }
                }
            """);
    }

    [Fact(Skip = "Not yet supported")]
    public void Selections_On_Interface_On_Node_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Product implements Node {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
              nodes(ids: [ID!]!): [Node]!
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                __typename
                id
                ... on ProductList {
                  products {
                    id
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }

    [Fact(Skip = "Not yet supported")]
    public void Selections_On_Interface_And_Concrete_Type_On_Node_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList {
              id: ID!
              products: [Product]
              singularProduct: Product
            }

            type Product implements Node {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
              nodes(ids: [ID!]!): [Node]!
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                __typename
                id
                ... on ProductList {
                  products {
                    id
                    name
                  }
                }
                ... on Item2 {
                  singularProduct {
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }
}
