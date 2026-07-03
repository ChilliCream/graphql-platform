using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ResolvedSelectionPathTests : FusionTestBase
{
    [Fact]
    public void Create_Should_ResolveTypeConditionAndPossibleTypes_When_ConditionIsKnownAbstractType()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: A
            type Query {
              orders: [OrderBase!]!
            }

            interface OrderBase {
              name: String!
            }

            interface MultiOrderBase implements OrderBase {
              name: String!
              items: [OrderItem!]!
            }

            type OrderA implements MultiOrderBase & OrderBase {
              name: String!
              items: [OrderItem!]!
            }

            type OrderB implements MultiOrderBase & OrderBase {
              name: String!
              items: [OrderItem!]!
            }

            type OrderItem {
              product: String!
            }
            """);

        var path = SelectionPath.Root
            .AppendField("orders")
            .AppendFragment("MultiOrderBase");

        // act
        var resolved = ResolvedSelectionPath.Create(path, schema);

        // assert
        Assert.Equal("MultiOrderBase", resolved.GetTypeCondition(1)?.Name);
        Assert.Equal(["OrderA", "OrderB"], resolved.GetPossibleTypes(1).Select(t => t.Name));
    }

    [Fact]
    public void Create_Should_FallBackToNameMatching_When_TypeConditionIsUnknown()
    {
        // arrange
        // A path referencing a type the schema cannot resolve, as can happen when a cached plan
        // outlives a schema change.
        var schema = ComposeSchema(
            """
            # name: A
            type Query {
              field: String
            }
            """);

        var path = SelectionPath.Root
            .AppendField("field")
            .AppendFragment("RemovedType");

        // act
        var resolved = ResolvedSelectionPath.Create(path, schema);

        // assert
        Assert.Null(resolved.GetTypeCondition(1));
        Assert.True(resolved.GetPossibleTypes(1).IsEmpty);
    }
}
