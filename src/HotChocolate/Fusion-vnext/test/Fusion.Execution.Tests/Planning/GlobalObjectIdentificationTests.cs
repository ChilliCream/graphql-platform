namespace HotChocolate.Fusion.Planning;

// TODO:
// - Conditional selections (on and below node field, also with conditional fragment)
// - Raw id argument value
// - Invalid Id
// - Valid Id, with unknown type
// - Valid Id, with known type, but not type of concrete type selection
// - Valid id, but subgraph returns other type for id
public class GlobalObjectIdentificationTests : FusionTestBase
{
    #region selections on node field

    [Fact]
    public void Node_Field_Just_Typename_Selected()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              title: Float!
            }
            """);

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Author implements Node @key(fields: "id") {
              id: ID!
              username: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                __typename
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Node_Field_Just_Id_And_Typename_Selected()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              title: Float!
            }
            """);

        var schema = ComposeSchema(source1);

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

    [Fact]
    public void Node_Field_Alongside_Regular_Root_Selections()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
              node(id: ID!): Node @lookup
              viewer: Viewer
            }

            type Viewer {
              username: String
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              title: Float!
            }
            """);

        var schema = ComposeSchema(source1);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id: ID!) {
              viewer {
                username
              }
              node(id: $id) {
                __typename
                ... on Discussion {
                  title
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Node_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "id") {
              id: ID!
              commentCount: Int
            }
            """);

        var schema = ComposeSchema(source1, source2);

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

    [Fact]
    public void Node_Field_Concrete_Type_Has_Dependency_Node_Field_Is_Aliased()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($id1: ID!, $id2: ID!) {
              a: node(id: $id1) {
                ... on Discussion {
                  name
                  commentCount
                }
              }
              b: node(id: $id2) {
                id
                ... on Discussion {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Node_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(id: ID!): Discussion @lookup
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

        var source2 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2);

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

    [Fact]
    public void Node_Field_Two_Concrete_Types_Selections_Have_Same_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var source3 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2, source3);

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

    [Fact]
    public void Node_Field_Two_Concrete_Types_Selections_Have_Different_Dependencies()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var source3 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2, source3);

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

    [Fact]
    public void Node_Field_Selections_On_Interface()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1);

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

    [Fact]
    public void Node_Field_Selections_On_Interface_And_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1);

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

    [Fact]
    public void Node_FIeld_Selections_On_Interface_And_Concrete_Type_Both_Have_Different_Dependencies()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2);

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

    [Fact]
    public void Node_Field_Selections_On_Interface_Selection_Has_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2);

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
