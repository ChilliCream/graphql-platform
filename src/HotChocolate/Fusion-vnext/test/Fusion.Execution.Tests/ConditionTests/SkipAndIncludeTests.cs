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
}
