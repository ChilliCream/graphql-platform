namespace HotChocolate.Fusion.Planning;

// TODO: Test with just __typename/id selection and singular and multiple concrete type selections with and without dependencies.
public class GlobalObjectIdentificationTests : FusionTestBase
{
    #region interface selection on node field

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
            NOT SUPPORTED
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
            NOT SUPPORTED
            """);
    }

    [Fact]
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

    [Fact]
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

    #endregion
}
