using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion;

public class ConditionTests : FusionTestBase
{
    [Test]
    public void Skip_On_SubField()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!, $skip: Boolean!) {
                    productById(id: $id) {
                      name @skip(if: $skip)
                      description
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_SubField_If_False()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                      description
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_SubField_If_True()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      description
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_SubField_Only_Skipped_Field_Selected()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!, $skip: Boolean!) {
                    productById(id: $id) {
                      name @skip(if: $skip)
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_SubField_Only_Skipped_Field_Selected_If_False()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_SubField_Only_Skipped_Field_Selected_If_True()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      __typename
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_SubField_Resolved_From_Other_Source()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                      id
                    }
                  }
              - id: 2
                schema: "REVIEWS"
                operation: >-
                  query($__fusion_requirement_1: ID!, $skip: Boolean!) {
                    productById(id: $__fusion_requirement_1) {
                      averageRating
                      reviews(first: 10) @skip(if: $skip) {
                        nodes {
                          body
                        }
                      }
                    }
                  }
                requirements:
                  - name: "__fusion_requirement_1"
                    dependsOn: "1"
                    selectionSet: "productById"
                    field: "id"
                    type: "ID!"

            """);
    }

    [Test]
    public void Skip_On_SubField_Resolved_From_Other_Source_If_False()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                      id
                    }
                  }
              - id: 2
                schema: "REVIEWS"
                operation: >-
                  query($__fusion_requirement_1: ID!) {
                    productById(id: $__fusion_requirement_1) {
                      averageRating
                      reviews(first: 10) {
                        nodes {
                          body
                        }
                      }
                    }
                  }
                requirements:
                  - name: "__fusion_requirement_1"
                    dependsOn: "1"
                    selectionSet: "productById"
                    field: "id"
                    type: "ID!"

            """);
    }

    [Test]
    public void Skip_On_SubField_Resolved_From_Other_Source_If_True()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                      id
                    }
                  }
              - id: 2
                schema: "REVIEWS"
                operation: >-
                  query($__fusion_requirement_1: ID!) {
                    productById(id: $__fusion_requirement_1) {
                      averageRating
                    }
                  }
                requirements:
                  - name: "__fusion_requirement_1"
                    dependsOn: "1"
                    selectionSet: "productById"
                    field: "id"
                    type: "ID!"

            """);
    }

    [Test]
    public void Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                      id
                    }
                  }
              - id: 2
                schema: "REVIEWS"
                operation: >-
                  query($__fusion_requirement_1: ID!) {
                    productById(id: $__fusion_requirement_1) {
                      reviews(first: 10) {
                        nodes {
                          body
                        }
                      }
                    }
                  }
                skipIf: "skip"
                requirements:
                  - name: "__fusion_requirement_1"
                    dependsOn: "1"
                    selectionSet: "productById"
                    field: "id"
                    type: "ID!"

            """);
    }

    [Test]
    public void Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected_If_False()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
            - id: 1
              schema: "PRODUCTS"
              operation: >-
                query($id: ID!) {
                  productById(id: $id) {
                    name
                    id
                  }
                }
            - id: 2
              schema: "REVIEWS"
              operation: >-
                query($__fusion_requirement_1: ID!) {
                  productById(id: $__fusion_requirement_1) {
                    reviews(first: 10) {
                      nodes {
                        body
                      }
                    }
                  }
                }
              requirements:
                - name: "__fusion_requirement_1"
                  dependsOn: "1"
                  selectionSet: "productById"
                  field: "id"
                  type: "ID!"

            """);
    }

    [Test]
    public void Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected_If_True()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_RootField()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!, $skip: Boolean!) {
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
    }

    [Test]
    public void Skip_On_RootField_If_False()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                    }
                    products {
                      nodes {
                        name
                      }
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_RootField_If_True()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  {
                    products {
                      nodes {
                        name
                      }
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_RootField_Only_Skipped_Field_Selected()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                    }
                  }
                skipIf: "skip"

            """);
    }

    [Test]
    public void Skip_On_RootField_Only_Skipped_Field_Selected_If_False()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                    }
                  }

            """);
    }

    [Test]
    public void Skip_On_RootField_Only_Skipped_Field_Selected_If_True()
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:

            """);
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $skip: Boolean!, $include: Boolean!) {
                productById(id: $id) @skip(if: $skip) @include(if: $include) {
                    name
                }
            }
            """);

        // assert
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!) {
                    productById(id: $id) {
                      name
                    }
                  }
                skipIf: "skip"

            """);
    }

    [Test]
    public void Skip_And_Include_On_RootField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
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
        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  query($id: ID!, $include: Boolean!, $skip: Boolean!) {
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
    }
}
