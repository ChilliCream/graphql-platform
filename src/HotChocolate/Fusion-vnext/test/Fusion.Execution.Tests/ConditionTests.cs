namespace HotChocolate.Fusion;

public class ConditionTests : FusionTestBase
{
    [Test]
    public async Task Skip_On_SubField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name @skip(if: $skip)
                    description
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { name @skip(if: $skip) description } }"
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_SubField_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: false)
                    description
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
    public async Task Skip_On_SubField_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: true)
                    description
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
    public async Task Skip_On_SubField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name @skip(if: $skip)
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { name @skip(if: $skip) } }"
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_SubField_Only_Skipped_Field_Selected_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: false)
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
    public async Task Skip_On_SubField_Only_Skipped_Field_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: true)
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name
                    averageRating
                    reviews(first: 10) @skip(if: $skip) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!) { productById(id: $id) { name } }",
                      "nodes": [
                        {
                          "kind": "Operation",
                          "schema": "REVIEWS",
                          "document": "query($skip: Boolean!) { productById { averageRating reviews(first: 10) @skip(if: $skip) { nodes { body } } } }"
                        }
                      ]
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    averageRating
                    reviews(first: 10) @skip(if: false) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!) { productById(id: $id) { name } }",
                      "nodes": [
                        {
                          "kind": "Operation",
                          "schema": "REVIEWS",
                          "document": "{ productById { averageRating reviews(first: 10) { nodes { body } } } }"
                        }
                      ]
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    averageRating
                    reviews(first: 10) @skip(if: true) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!) { productById(id: $id) { name } }",
                      "nodes": [
                        {
                          "kind": "Operation",
                          "schema": "REVIEWS",
                          "document": "{ productById { averageRating } }"
                        }
                      ]
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name
                    reviews(first: 10) @skip(if: $skip) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!) { productById(id: $id) { name } }",
                      "nodes": [
                        {
                          "kind": "Condition",
                          "variableName": "skip",
                          "passingValue": false,
                          "nodes": [
                            {
                              "kind": "Operation",
                              "schema": "REVIEWS",
                              "document": "{ productById { reviews(first: 10) { nodes { body } } } }"
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
    public async Task Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    reviews(first: 10) @skip(if: false) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!) { productById(id: $id) { name } }",
                      "nodes": [
                        {
                          "kind": "Operation",
                          "schema": "REVIEWS",
                          "document": "{ productById { reviews(first: 10) { nodes { body } } } }"
                        }
                      ]
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    reviews(first: 10) @skip(if: true) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
    public async Task Skip_On_RootField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) @skip(if: $skip) {
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
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root",
                  "nodes": [
                    {
                      "kind": "Operation",
                      "schema": "PRODUCTS",
                      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) @skip(if: $skip) { name } products { nodes { name } } }"
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_RootField_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) {
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
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
    public async Task Skip_On_RootField_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) {
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
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
    public async Task Skip_On_RootField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) @skip(if: $skip) {
                    name
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
                          "document": "{ productById(id: $id) { name } }"
                        }
                      ]
                    }
                  ]
                }
                """);
    }

    [Test]
    public async Task Skip_On_RootField_Only_Skipped_Field_Selected_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) {
                    name
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
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
    public async Task Skip_On_RootField_Only_Skipped_Field_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) {
                    name
                }
            }
            """);

        // assert
        await Assert
            .That(plan.Serialize())
            .IsEqualTo(
                """
                {
                  "kind": "Root"
                }
                """);
    }
}
