namespace HotChocolate.Fusion;

public class SkipFragmentTests : FusionTestBase
{
    [Test]
    public void Skip_On_Root_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                ...QueryFragment @skip(if: $skip)
                products {
                  nodes {
                    name
                  }
                }
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
                    name
                }
            }
            """);

        // assert
        plan.Serialize().MatchInlineSnapshot(
            """
            {
              "kind": "Root",
              "nodes": [
                {
                  "kind": "Operation",
                  "schema": "PRODUCTS",
                  "document": "query($id: ID!) { ... on Query @skip(if: $skip) { productById(id: $id) { name } } products { nodes { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_Root_Fragment_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: false)
                products {
                  nodes {
                    name
                  }
                }
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
                    name
                }
            }
            """);

        // assert
        plan.Serialize().MatchInlineSnapshot(
            """
            {
              "kind": "Root",
              "nodes": [
                {
                  "kind": "Operation",
                  "schema": "PRODUCTS",
                  "document": "query($id: ID!) { ... on Query { productById(id: $id) { name } } products { nodes { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_Root_Fragment_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: true)
                products {
                  nodes {
                    name
                  }
                }
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
                    name
                }
            }
            """);

        // assert
        plan.Serialize().MatchInlineSnapshot(
            """
            {
              "kind": "Root",
              "nodes": [
                {
                  "kind": "Operation",
                  "schema": "PRODUCTS",
                  "document": "{ products { nodes { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_Root_Fragment_Only_Skipped_Fragment_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                ...QueryFragment @skip(if: $skip)
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
                    name
                }
            }
            """);

        // assert
        plan.Serialize().MatchInlineSnapshot(
            """
            {
              "kind": "Root",
              "nodes": [
                {
                  "kind": "Condition",
                  "variableName": "skip",
                  "passingValue": false,
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "{ ... on Query { productById(id: $id) { name } } }"
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_Root_Fragment_Only_Skipped_Fragment_Selected_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: false)
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
                    name
                }
            }
            """);

        // assert
        plan.Serialize().MatchInlineSnapshot(
            """
            {
              "kind": "Root",
              "nodes": [
                {
                  "kind": "Operation",
                  "schema": "PRODUCTS",
                  "document": "query($id: ID!) { ... on Query { productById(id: $id) { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_Root_Fragment_Only_Skipped_Fragment_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: true)
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
                    name
                }
            }
            """);

        // assert
        plan.Serialize().MatchInlineSnapshot(
            """
            {
              "kind": "Root"
            }
            """);
    }
}
