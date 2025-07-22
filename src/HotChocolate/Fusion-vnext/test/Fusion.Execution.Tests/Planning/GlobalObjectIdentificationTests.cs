namespace HotChocolate.Fusion.Planning;

// TODO once execution is implemented:
// - Invalid Id
// - Valid Id, with unknown type
// - Valid Id, with known type, but not type of concrete type selection
public class GlobalObjectIdentificationTests : FusionTestBase
{
    #region selections on node field

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Id_And_Typename_Selection()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }

            type Comment implements Node @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              name: String
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              viewerRating: Float!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Two_Concrete_Types_Selections_Have_Same_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Review implements Node @key(fields: "id") {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphC = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Two_Concrete_Types_Selections_Have_Different_Dependencies()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Review implements Node @key(fields: "id") {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphC = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    #endregion

    #region interface selections on node field

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Selections_On_Interface()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Selections_On_Interface_And_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Not yet supported")]
    public void Node_FIeld_Selections_On_Interface_And_Concrete_Type_Both_Have_Different_Dependencies()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList @key(fields: "id") {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList @key(fields: "id") {
              id: ID!
              products: [Product]
              singularProduct: Product
            }

            type Product implements Node @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Not yet supported")]
    public void Node_Field_Selections_On_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList @key(fields: "id") {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList @key(fields: "id") {
              id: ID!
              products: [Product]
            }

            type Product implements Node @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @key(fields: "id") {
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
        MatchSnapshot(plan);
    }

    #endregion
}
