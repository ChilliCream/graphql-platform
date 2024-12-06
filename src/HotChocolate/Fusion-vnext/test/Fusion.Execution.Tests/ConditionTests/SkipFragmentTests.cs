namespace HotChocolate.Fusion;

public class SkipFragmentTests : FusionTestBase
{
    [Test]
    public void Skip_On_RootFragment()
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
                  "document": "query($id: ID!, $skip: Boolean!) { ... on Query @skip(if: $skip) { productById(id: $id) { name } } products { nodes { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_RootFragment_If_False()
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
                  "document": "query($id: ID!) { productById(id: $id) { name } products { nodes { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_RootFragment_If_True()
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
    public void Skip_On_RootFragment_Only_Skipped_Fragment_Selected()
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
    public void Skip_On_RootFragment_Only_Skipped_Fragment_Selected_If_False()
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
                  "document": "query($id: ID!) { productById(id: $id) { name } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_RootFragment_Only_Skipped_Fragment_Selected_If_True()
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

    [Test]
    public void Skip_On_SubFragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    ...ProductFragment @skip(if: $skip)
                    description
                }
            }

            fragment ProductFragment on Product {
                name
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
                  "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { ... on Product @skip(if: $skip) { name } description } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_SubFragment_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment @skip(if: false)
                    description
                }
            }

            fragment ProductFragment on Product {
                name
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
                  "document": "query($id: ID!) { productById(id: $id) { name description } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_SubFragment_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment @skip(if: true)
                    description
                }
            }

            fragment ProductFragment on Product {
                name
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
                  "document": "query($id: ID!) { productById(id: $id) { description } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_SubFragment_Only_Skipped_Fragment_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    ...ProductFragment @skip(if: $skip)
                }
            }

            fragment ProductFragment on Product {
                name
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
                  "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { ... on Product @skip(if: $skip) { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_SubFragment_Only_Skipped_Fragment_SelectedIf_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment @skip(if: false)
                }
            }

            fragment ProductFragment on Product {
                name
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
                  "document": "query($id: ID!) { productById(id: $id) { name } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_On_SubFragment_Only_Skipped_Fragment_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment @skip(if: true)
                }
            }

            fragment ProductFragment on Product {
                name
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
                  "document": "query($id: ID!) { productById(id: $id) { __typename } }"
                }
              ]
            }
            """);
    }
}
