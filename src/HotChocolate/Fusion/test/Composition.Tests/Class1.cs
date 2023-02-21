using CookieCrumble;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion.Composition;

public class Class1
{
    [Fact]
    public async Task Test()
    {
        var sdl =
            """
            type Query {
              customer(id: ID!): Customer
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
                @remove(coordinate: "Query.customerOrConsultant")
                @remove(coordinate: "Consultant") {
              query: Query
            }

            scalar String
            scalar ID
            directive @rename(coordinate: String! to: String!) on SCHEMA
            directive @remove(coordinate: String!) on SCHEMA
            """;

        var composer = new FusionGraphComposer(Array.Empty<IObjectTypeMetaDataEnricher>());
        var graph = await composer.ComposeAsync(new SubGraphConfiguration("Abc", sdl));

        SchemaFormatter
            .FormatAsString(graph)
            .MatchInlineSnapshot(
                """
                type Query {
                  customer(id: ID!): Customer
                }

                type Customer {
                  complexArg(arg: ComplexInputType): String
                  id: ID!
                  name: String!
                }

                union CustomerOrConsultant = Customer

                input ComplexInputType {
                  deeper: ComplexInputType
                  deeperArray: [ComplexInputType]
                  value: String
                  valueArray: [String]
                }

                scalar String

                scalar ID

                directive @rename on

                directive @remove on
                """);
    }
}
