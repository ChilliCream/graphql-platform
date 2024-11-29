using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class DeprecationMergeTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Merge_Entity_Output_Field_Deprecation()
        => await Succeed(
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand implements Node {
              id: ID!
              name: String! @deprecated(reason: "Some reason")
            }

            interface Node {
              id: ID!
            }
            """,
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand implements Node {
              id: ID!
              newName: String!
            }

            interface Node {
              id: ID!
            }
            """);

    [Fact]
    public async Task Merge_Entity_Output_Field_Argument_Deprecation()
        => await Succeed(
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand implements Node {
              id: ID!
              name(includeFirstName: Boolean @deprecated(reason: "Some reason")): String!
            }

            interface Node {
              id: ID!
            }
            """,
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand implements Node {
              id: ID!
              name(includeFirstName: Boolean): String!
            }

            interface Node {
              id: ID!
            }
            """);

    [Fact]
    public async Task Merge_Output_Field_Deprecation()
        => await Succeed(
            """
            type Query {
              brand: Brand
            }

            type Brand {
              name: String! @deprecated(reason: "Some reason")
            }
            """,
            """
            type Query {
              brand: Brand
            }

            type Brand {
              newName: String!
            }
            """);

    [Fact]
    public async Task Merge_Output_Field_Argument_Deprecation()
        => await Succeed(
            """
            type Query {
              brand: Brand
            }

            type Brand {
              name(includeFirstName: Boolean @deprecated(reason: "Some reason")): String!
            }
            """,
            """
            type Query {
              brand: Brand
            }

            type Brand {
              name(includeFirstName: Boolean): String!
            }
            """);

    [Fact]
    public async Task Merge_Enum_Value_Deprecation()
        => await Succeed(
            """
            type Query {
              value: OrderStatus
            }

            enum OrderStatus {
              SENT_OUT @deprecated(reason: "Some reason")
            }
            """,
            """
            type Query {
              value: OrderStatus
            }

            enum OrderStatus {
              SHIPPED
            }
            """);
}
