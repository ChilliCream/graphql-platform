using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion;

public class FieldRequirementsTests : FusionTestBase
{
    [Fact]
    public void Simple_Field_Requirement()
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
              ) {
              id: ID!
                @fusion__field(schema: PRODUCTS)
                @fusion__field(schema: SHIPPING)
              name: String!
                @fusion__field(schema: SHIPPING)
              someField: String!
                @fusion__field(schema: PRODUCTS)
                @fusion__requires(
                  schema: PRODUCTS
                  field: "someField(name: String!): String!"
                  map: ["name"]
                )
            }
            """);

        var plan = PlanOperation(
            """
            {
                productById(id: 1) {
                    someField
                }
            }
            """,
            schema);

        plan.ToYaml().MatchInlineSnapshot(
            """

            """);
    }
}
