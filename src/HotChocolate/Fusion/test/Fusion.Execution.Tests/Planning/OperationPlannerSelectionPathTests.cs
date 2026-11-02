using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlannerSelectionPathTests
{
    [Fact]
    public void ContainsSelectionsAtPath_Should_NotMatch_When_RuntimeTypeIsSibling()
    {
        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
            """
            {
                media {
                    ... on Book {
                        animals {
                            ... on Cat {
                                id
                            }
                        }
                    }
                }
            }
            """);
        var required = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");

        var isCovered = OperationPlanner.ContainsSelectionsAtPath(
            selectionSet,
            SelectionPath.Parse("$.media<Book>.animals<Dog>"),
            required);

        Assert.False(isCovered);
    }

    [Fact]
    public void ContainsSelectionsAtPath_Should_NotMatch_When_RuntimeTypeIsConditional()
    {
        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
            """
            {
                media {
                    ... on Book {
                        animals {
                            ... on Cat @include(if: true) {
                                id
                            }
                        }
                    }
                }
            }
            """);
        var required = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");

        var isCovered = OperationPlanner.ContainsSelectionsAtPath(
            selectionSet,
            SelectionPath.Parse("$.media<Book>.animals<Cat>"),
            required);

        Assert.False(isCovered);
    }
}
