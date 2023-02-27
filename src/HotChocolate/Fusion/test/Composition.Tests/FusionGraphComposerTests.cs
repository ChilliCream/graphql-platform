using CookieCrumble;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Fusion.Composition.ComposerFactory;

namespace HotChocolate.Fusion.Composition;

public class FusionGraphComposerTests
{
    [Fact]
    public async Task Infer_Customer_ById_Resolver()
    {
        var subGraphA =
            """
            type Query {
              customer(id: ID! @ref(field: "id")): Customer
              consultant(id: ID!): Consultant
              customerOrConsultant(id: ID!): CustomerOrConsultant
            }

            type Customer {
              id: ID!
              name: String!
              consultant: Consultant
              complexArg(arg: ComplexInputType): String
            }

            type Consultant {
              id: ID!
              name: String!
            }

            union CustomerOrConsultant = Customer | Consultant

            input ComplexInputType {
              deeper: ComplexInputType
              deeperArray: [ComplexInputType]
              value: String
              valueArray: [String]
            }

            schema
                @remove(coordinate: "CustomerOrConsultant")
                @remove(coordinate: "Query.customerOrConsultant")
                @remove(coordinate: "Consultant") {
              query: Query
            }

            scalar String
            scalar ID
            directive @rename(coordinate: String! to: String!) on SCHEMA
            directive @remove(coordinate: String!) on SCHEMA
            directive @ref(coordinate: String, field: String) on FIELD_DEFINITION
            """;

        var subGraphB =
            """
            type Query {
              customer(id: ID! @ref(field: "id")): Customer
            }

            type Customer {
              id: ID!
              notes: String!
            }

            schema {
              query: Query
            }

            scalar String
            scalar ID
            directive @ref(coordinate: String, field: String) on FIELD_DEFINITION
            """;

        var composer = CreateComposer();
        var context = await composer.ComposeAsync(
            new SubGraphConfiguration("a", subGraphA),
            new SubGraphConfiguration("b", subGraphB));

        SchemaFormatter
            .FormatAsString(context.FusionGraph)
            .MatchInlineSnapshot(
                """
                input ComplexInputType {
                  deeper: ComplexInputType
                  deeperArray: [ComplexInputType]
                  value: String
                  valueArray: [String]
                }

                type Customer {
                  complexArg(arg: ComplexInputType): String
                  id: ID!
                  name: String!
                  notes: String!
                }

                scalar ID

                scalar String
                """);
    }
}
