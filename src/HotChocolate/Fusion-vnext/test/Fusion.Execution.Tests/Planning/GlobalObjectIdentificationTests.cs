namespace HotChocolate.Fusion.Planning;

// TODO once execution is implemented:
// - Invalid Id
// - Valid Id, with unknown type
// - Valid Id, with known type, but not type of concrete type selection
public class GlobalObjectIdentificationTests : FusionTestBase
{
    #region selections on node field

    [Fact]
    public void Id_And_Typename_Selection_On_Node_Field()
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

            type Discussion implements Node {
              id: ID!
              viewerRating: Float!
            }

            type Comment implements Node {
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
                id
                __typename
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
    public void Concrete_Type_On_Node_Field_Selection_Has_Dependency()
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

            type Discussion implements Node {
              id: ID!
              viewerRating: Float!
              product: Product
            }

            type Product {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
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
                ... on Discussion {
                  id
                  viewerRating
                  product {
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
    public void Concrete_Type_On_Node_Field_With_Dependency()
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

            type Discussion implements Node {
              id: ID!
              name: String
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int
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
                ... on Discussion {
                  name
                  commentCount
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
    public void Two_Concrete_Types_On_Node_Field_Selection_Has_Same_Dependency()
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

            type Discussion implements Node {
              id: ID!
              product: Product
            }

            type Product {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Review implements Node {
              id: ID!
              product: Product
            }

            type Product {
              id: ID!
            }
            """);

        var subgraphC = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB, subgraphC);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Discussion {
                  product {
                    name
                  }
                }
                ... on Review {
                  product {
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
    public void Two_Concrete_Types_On_Node_Field_Selection_Has_Different_Dependency()
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

            type Discussion implements Node {
              id: ID!
              product: Product
            }

            type Product {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Review implements Node {
              id: ID!
              product: Product
            }

            type Product {
              id: ID!
            }
            """);

        var subgraphC = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB, subgraphC);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Discussion {
                  product {
                    id
                    name
                  }
                }
                ... on Review {
                  product {
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

    #region interface selections on node field

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
