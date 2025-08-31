namespace HotChocolate.Fusion.Planning;

public class NodeLookupTests : FusionTestBase
{
    [Fact]
    public void Requirement_SelectionMap_Object_Shop()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query findMe {
              users {
                nodes {
                  id
                  birthdate
                  reviews {
                    nodes {
                      author {
                        id
                        name
                      }
                      product {
                        id
                        weight
                        deliveryEstimate(zip: "4383")
                        pictureFileName
                        price
                        quantity
                      }
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}
