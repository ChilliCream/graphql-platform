using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlannerSelectionInliningTests : FusionTestBase
{
    [Fact]
    public void InlineSelections_Should_RewriteAllMatchingSelectionSets_When_SelectionSetsShareTargetId()
    {
        // arrange
        var (planner, schema) = CreatePlanner();
        var operation = ParseOperation();
        var (index, root, left, nested, right) = CreateMergedIndex(operation);
        var targetId = index.GetId(left);
        var selectionsToInline = ParseSelections("{ extra }");

        // act
        var rewritten = planner.InlineSelections(
            operation,
            index,
            schema.Types.GetType<FusionObjectTypeDefinition>("Item"),
            targetId,
            selectionsToInline);

        // assert
        FormatResult(rewritten, index, root, targetId).MatchInlineSnapshot(
            """
            query Test {
              left {
                child {
                  original
                  extra
                }
                extra
              }
              right {
                original
                extra
              }
              untouched {
                ...Shared @include(if: true)
              }
            }
            rootId: 1
            rewrittenRootId: 1
            targetId: 2
            leftId: 2
            nestedId: 2
            rightId: 2
            allRegistered: true
            """);
    }

    [Fact]
    public void InlineSelections_Should_PreserveInternalPostOrderSemantics_When_NestedSelectionSetsShareTargetId()
    {
        // arrange
        var (planner, schema) = CreatePlanner();
        var operation = ParseOperation();
        var (index, root, left, _, _) = CreateMergedIndex(operation);
        var targetId = index.GetId(left);
        var selectionsToInline = ParseSelections("{ extra }");

        // act
        var rewritten = planner.InlineSelections(
            operation,
            index,
            schema.Types.GetType<FusionObjectTypeDefinition>("Item"),
            targetId,
            selectionsToInline,
            inlineInternal: true);

        // assert
        FormatResult(rewritten, index, root, targetId).MatchInlineSnapshot(
            """
            query Test {
              left {
                child {
                  original
                }
                extra @fusion__requirement
              }
              right {
                original
                extra @fusion__requirement
              }
              untouched {
                ...Shared @include(if: true)
              }
            }
            rootId: 1
            rewrittenRootId: 1
            targetId: 2
            leftId: 2
            nestedId: 2
            rightId: 2
            allRegistered: true
            """);
    }

    private static (OperationPlanner Planner, FusionSchemaDefinition Schema) CreatePlanner()
    {
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              left: Item
              right: Item
              untouched: Item
            }

            type Item {
              original: String
              extra: String
              child: Item
            }
            """);
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        return (new OperationPlanner(schema, compiler), schema);
    }

    private static OperationDefinitionNode ParseOperation()
        => Utf8GraphQLParser
            .Parse(
                """
                query Test {
                  left {
                    child {
                      original
                    }
                  }
                  right {
                    original
                  }
                  untouched {
                    ...Shared @include(if: true)
                  }
                }
                """)
            .GetOperation("Test");

    private static SelectionSetNode ParseSelections(string source)
        => Utf8GraphQLParser.Parse(source).GetOperation(null).SelectionSet;

    private static (
        SelectionSetIndexBuilder Index,
        SelectionSetNode Root,
        SelectionSetNode Left,
        SelectionSetNode Nested,
        SelectionSetNode Right) CreateMergedIndex(OperationDefinitionNode operation)
    {
        var root = operation.SelectionSet;
        var left = ((FieldNode)root.Selections[0]).SelectionSet!;
        var nested = ((FieldNode)left.Selections[0]).SelectionSet!;
        var right = ((FieldNode)root.Selections[1]).SelectionSet!;
        var index = SelectionSetIndexer.Create(operation).ToBuilder();

        index.OnMerge(left, nested);
        index.OnMerge(left, right);

        return (index, root, left, nested, right);
    }

    private static string FormatResult(
        OperationDefinitionNode operation,
        SelectionSetIndexBuilder index,
        SelectionSetNode originalRoot,
        uint targetId)
    {
        var root = operation.SelectionSet;
        var left = ((FieldNode)root.Selections[0]).SelectionSet!;
        var nested = ((FieldNode)left.Selections[0]).SelectionSet!;
        var right = ((FieldNode)root.Selections[1]).SelectionSet!;
        var allRegistered = SelectionSetIndexer
            .CreateIdSet(root, index)
            .All(id => index.TryGetSelectionSet(id, out _));

        return operation.ToString(indented: true)
            + $"\nrootId: {index.GetId(originalRoot)}"
            + $"\nrewrittenRootId: {index.GetId(root)}"
            + $"\ntargetId: {targetId}"
            + $"\nleftId: {index.GetId(left)}"
            + $"\nnestedId: {index.GetId(nested)}"
            + $"\nrightId: {index.GetId(right)}"
            + $"\nallRegistered: {allRegistered.ToString().ToLowerInvariant()}";
    }
}
