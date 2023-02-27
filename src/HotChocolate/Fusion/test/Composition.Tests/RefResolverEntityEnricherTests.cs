using CookieCrumble;
using static HotChocolate.Fusion.Composition.ComposerFactory;

namespace HotChocolate.Fusion.Composition;

public class RefResolverEntityEnricherTests
{
    [Fact]
    public async Task Infer_Customer_ById_Resolver()
    {
        var sdl =
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

        var composer = CreateComposer();
        var context = await composer.ComposeAsync(new SubGraphConfiguration("Abc", sdl));

        var snapshot = new Snapshot();

        foreach (var entity in context.Entities)
        {
            snapshot.Add(entity.Metadata.ToString(), name: entity.Name);
        }

        snapshot.MatchInline(
            """
            query($Customer_id: ID!) @schema(name: "Abc") {
              customer(id: $Customer_id)
            }

            fragment Requirements on Customer {
              id @variable(name: "Customer_id")
            }

            """);
    }

    [Fact]
    public async Task Infer_Customer_TwoArg_Resolver()
    {
        var sdl =
            """
            type Query {
              customer(id: ID! @ref(field: "id")): Customer
              customerWithCid(
                id: ID! @ref(field: "id")
                consultantId: ID @ref(field: "consultant { id }")): Customer
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

        var composer = CreateComposer();
        var context = await composer.ComposeAsync(new SubGraphConfiguration("Abc", sdl));

        var snapshot = new Snapshot();

        foreach (var entity in context.Entities)
        {
            snapshot.Add(entity.Metadata.ToString(), name: entity.Name);
        }

        snapshot.MatchInline(
            """
            query($Customer_id: ID!) @schema(name: "Abc") {
              customer(id: $Customer_id)
            }

            fragment Requirements on Customer {
              id @variable(name: "Customer_id")
            }

            query($Customer_consultant_id: ID, $Customer_id: ID!) @schema(name: "Abc") {
              customerWithCid(consultantId: $Customer_consultant_id, id: $Customer_id)
            }

            fragment Requirements on Customer {
              consultant @variable(name: "Customer_consultant_id") {
                id
              }
              id @variable(name: "Customer_id")
            }

            """);
    }
}
