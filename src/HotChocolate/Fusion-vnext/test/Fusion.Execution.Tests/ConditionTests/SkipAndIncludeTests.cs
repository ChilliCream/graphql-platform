namespace HotChocolate.Fusion;

public class SkipAndIncludeTests : FusionTestBase
{
    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!, $include: Boolean!) {
                productById(id: $id) @skip(if: $skip) @include(if: $include) {
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
                      "kind": "Condition",
                      "variableName": "include",
                      "passingValue": true,
                      "nodes": [
                        {
                          "kind": "Operation",
                          "schema": "PRODUCTS",
                          "document": "{ productById(id: $id) { name } }"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) @include(if: false) {
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
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) @include(if: true) {
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
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) @include(if: true) {
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
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) @include(if: false) {
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
    public void Skip_And_Include_On_RootField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!, $include: Boolean!) {
                productById(id: $id) @skip(if: $skip) @include(if: $include) {
                    name
                }
                products {
                    nodes {
                        name
                    }
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
                  "document": "query($id: ID!, $include: Boolean!, $skip: Boolean!) { productById(id: $id) @skip(if: $skip) @include(if: $include) { name } products { nodes { name } } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Skip_And_Include_On_RootField_Skip_True_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) @include(if: false) {
                    name
                }
                products {
                    nodes {
                        name
                    }
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
    public void Skip_And_Include_On_RootField_Skip_False_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) @include(if: true) {
                    name
                }
                products {
                    nodes {
                        name
                    }
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
    public void Skip_And_Include_On_RootField_Skip_True_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) @include(if: true) {
                    name
                }
                products {
                    nodes {
                        name
                    }
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
    public void Skip_And_Include_On_RootField_Skip_False_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) @include(if: false) {
                    name
                }
                products {
                    nodes {
                        name
                    }
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
}
