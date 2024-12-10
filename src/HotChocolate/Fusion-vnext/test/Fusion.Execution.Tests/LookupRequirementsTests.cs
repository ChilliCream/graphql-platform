using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion;

public class LookupRequirementsTests : FusionTestBase
{
    [Test]
    public void Key_Has_Requirement_To_Schema_That_Is_Not_In_Context()
    {
        var schema = CreateCompositeSchema(
            """
            type Query {
              productById(id: ID!): Product
                @fusion__field(schema: PRODUCTS)
            }

            type Product
              @fusion__type(schema: PRODUCTS)
              @fusion__type(schema: SHIPPING)
              @fusion__type(schema: REVIEWS)
              @fusion__lookup(
                  schema: PRODUCTS
                  key: "{ id }"
                  field: "productById(id: ID!): Product"
                  map: ["id"]
              )
              @fusion__lookup(
                  schema: SHIPPING
                  key: "{ id }"
                  field: "productById(id: ID!): Product"
                  map: ["id"]
              )
              @fusion__lookup(
                  schema: REVIEWS
                  key: "{ id }"
                  field: "productById(id: ID!): Product"
                  map: ["id"]
              ) {
              id: ID!
                @fusion__field(schema: PRODUCTS)
                @fusion__field(schema: SHIPPING)
                @fusion__field(schema: REVIEWS)
              internalId: String!
                @fusion__field(schema: SHIPPING)
              internalSomeOther: String!
                @fusion__field(schema: REVIEWS)
                @fusion__requires(
                  schema: SHIPPING
                  field: "internalSomeOther(internalId: String!): String!"
                  map: ["internalId"]
                )
            }
            """);

        var plan = PlanOperationAsync(
            schema,
            """
            {
                productById(id: 1) {
                    internalSomeOther
                }
            }
            """);

        plan.ToYaml().MatchInlineSnapshot(
            """
            nodes:
              - id: 1
                schema: "PRODUCTS"
                operation: >-
                  {
                    productById(id: 1) {
                      id
                    }
                  }
              - id: 2
                schema: "SHIPPING
                operation: >-
                  {
                    productById(id: 1) {
                      internalId
                    }
                  }
                requirements:
                  - name: "__fusion_requirement_1"
                    dependsOn: "1"
                    selectionSet: "productById"
                    field: "id"
                    type: "ID!"
              - id: 3
                schema: "SHIPPING"
                operation: >-
                  query($__fusion_requirement_1: ID!, $__fusion_requirement_2: String!) {
                    productById(id: $__fusion_requirement_1) {
                      internalSomeOther(internalId: $__fusion_requirement_2)
                    }
                  }
                requirements:
                  - name: "__fusion_requirement_1"
                    dependsOn: "1"
                    selectionSet: "productById"
                    field: "id"
                    type: "ID!"
                  - name: "__fusion_requirement_2"
                    dependsOn: "2"
                    selectionSet: "productById"
                    field: "internalId"
                    type: "String!"
            """);
    }
}
