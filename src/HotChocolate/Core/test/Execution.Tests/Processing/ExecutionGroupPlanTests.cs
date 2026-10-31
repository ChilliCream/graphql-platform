namespace HotChocolate.Execution.Processing;

public class ExecutionGroupPlanTests
{
    [Fact]
    public async Task Create_Should_OrderSlotsByPriorityThenSelectionOrder_When_SelectionsUseAliasesAndFragments()
    {
        // arrange
        var plan = await CreatePlanAsync(
            """
            {
                connection {
                    ...Connection
                    pageInfo { hasNextPage }
                    b: nodes { name }
                    ... @defer { c: nodes { name } }
                }
            }

            fragment Connection on Connection {
                a: nodes { name }
                edges
            }
            """);

        // act
        var slots = plan.Slots.Select(s => $"{s.Selection.ResponseName} {s.Priority} {s.IsStreamable}");

        // assert
        slots.MatchInlineSnapshots(
        [
            "a 0 True",
            "b 0 True",
            "c 0 True",
            "edges 1 False",
            "hasNextPage 2 False"
        ]);
    }

    [Fact]
    public async Task Create_Should_ResolveNodesOfSelectionsOnTheWayToMembers_When_PathPassesThroughFields()
    {
        // arrange
        var (operation, rootSet, plan) = await ExecutionGroupTestOperation.CompileAsync(
            "{ connection { nodes { name } pageInfo { hasNextPage probe } probe } }");
        var pageInfo = rootSet.Selections.ToArray().Single(s => s.Field.Name is "pageInfo");

        // act
        var participants = rootSet.Selections.ToArray()
            .Concat(operation.GetSelectionSet(pageInfo).Selections.ToArray())
            .Select(s => $"{s.DeclaringType.Name}.{s.Field.Name} {plan.TryGetNode(s.Id, out _)}");

        // assert
        participants.MatchInlineSnapshots(
        [
            "Connection.nodes True",
            "Connection.pageInfo True",
            "Connection.probe False",
            "PageInfo.hasNextPage True",
            "PageInfo.probe False"
        ]);
    }

    [Fact]
    public async Task Create_Should_KeepPureAncestorsPure_When_OnlyMembersMustRunAsTasks()
    {
        // arrange
        var (operation, rootSet, plan) = await ExecutionGroupTestOperation.CompileAsync(
            "{ pureInfo { nodes { name } pageInfo { hasNextPage probe } } }");
        var pageInfo = rootSet.Selections.ToArray().Single(s => s.Field.Name is "pageInfo");

        // act
        var strategies = rootSet.Selections.ToArray()
            .Concat(operation.GetSelectionSet(pageInfo).Selections.ToArray())
            .Select(s => $"{s.DeclaringType.Name}.{s.Field.Name} {s.Strategy} {IsMember(plan, s)}");

        // assert
        strategies.MatchInlineSnapshots(
        [
            "PureInfoConnection.nodes Default True",
            "PureInfoConnection.pageInfo Pure False",
            "PageInfo.hasNextPage Default True",
            "PageInfo.probe Default False"
        ]);
    }

    [Fact]
    public async Task Create_Should_CompileNestedSelectionSetsWithConsistentIds_When_NestedSetsGrowTheElementArray()
    {
        // arrange
        var aliases = string.Join(" ", Enumerable.Range(0, 150).Select(i => $"a{i}: hasNextPage"));
        var (operation, rootSet, plan) = await ExecutionGroupTestOperation.CompileAsync(
            $"{{ connection {{ nodes {{ name }} pageInfo {{ {aliases} }} }} }}");
        var pageInfo = rootSet.Selections.ToArray().Single(s => s.Field.Name is "pageInfo");

        // act
        var nestedSet = operation.GetSelectionSet(pageInfo);
        var selections = rootSet.Selections.ToArray().Concat(nestedSet.Selections.ToArray()).ToArray();

        // assert
        Assert.All(selections, s => Assert.Same(s, operation.GetSelectionById(s.Id)));
        Assert.Same(nestedSet, operation.GetSelectionSetById(nestedSet.Id));
        Assert.Equal(150, plan.Slots.Count(s => ReferenceEquals(s.Selection.DeclaringSelectionSet, nestedSet)));
    }

    private static bool IsMember(ExecutionGroupPlan plan, Selection selection)
        => plan.TryGetNode(selection.Id, out var node) && node.SlotIndex >= 0;

    private static async Task<ExecutionGroupPlan> CreatePlanAsync(string query)
    {
        var (_, _, plan) = await ExecutionGroupTestOperation.CompileAsync(query);
        return plan;
    }
}
