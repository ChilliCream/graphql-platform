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
                scalar _Selection

                scalar _SelectionSet

                scalar _Type

                scalar _TypeName

                input ComplexInputType {
                  deeper: ComplexInputType
                  deeperArray: [ComplexInputType]
                  value: String
                  valueArray: [String]
                }

                type Customer @resolver(subGraphName: "a", select: "{\n  customer(id: $Customer_id)\n}") @variable(subGraphName: "a", name: "Customer_id", select: "id", type: "ID!") @resolver(subGraphName: "b", select: "{\n  customer(id: $Customer_id)\n}") @variable(subGraphName: "b", name: "Customer_id", select: "id", type: "ID!") {
                  complexArg(arg: ComplexInputType): String @source(subGraphName: "a")
                  id: ID! @source(subGraphName: "a") @source(subGraphName: "b")
                  name: String! @source(subGraphName: "a")
                  notes: String! @source(subGraphName: "b")
                }

                scalar ID

                type Query {
                  customer(id: ID!): Customer @resolver(subGraphName: "a", select: "{\n  customer(id: $id)\n}") @resolver(subGraphName: "b", select: "{\n  customer(id: $id)\n}")
                }

                scalar String

                directive @resolver(select: _TypeName! subGraphName: _SelectionSet!) on OBJECT

                directive @source(name: _TypeName subGraphName: _TypeName!) on FIELD_DEFINITION

                directive @variable(name: _TypeName! select: _Selection! subGraphName: _TypeName! type: _Type!) on OBJECT | FIELD_DEFINITION

                schema {
                  query: Query
                }
                """);
    }
}
