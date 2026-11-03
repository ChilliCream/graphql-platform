using System.Collections.Immutable;
using System.Text;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

public class ExecutionGroupScopeTests
{
    private const string Query = "{ connection { nodes { name } edges pageInfo { hasNextPage } } }";

    [Fact]
    public async Task Open_Should_StartAtTheFirstSlot_When_AllMembersAreIncluded()
    {
        // arrange
        // act
        var scope = await OpenAsync(Query);

        // assert
        Assert.Equal(0, scope!.Position);
    }

    [Fact]
    public async Task Open_Should_ReturnNull_When_FewerThanTwoMembersAreIncluded()
    {
        // arrange
        // act
        var scope = await OpenAsync(
            "query ($skip: Boolean!) { connection { nodes @skip(if: $skip) { name } edges } }",
            skip: true);

        // assert
        Assert.Null(scope);
    }

    [Fact]
    public async Task Open_Should_SkipMembers_When_RequestExcludesThem()
    {
        // arrange
        // act
        var scope = await OpenAsync(
            """
            query ($skip: Boolean!) {
                connection {
                    nodes @skip(if: $skip) { name }
                    edges
                    pageInfo { hasNextPage }
                }
            }
            """,
            skip: true);

        // assert
        Assert.Equal(1, scope!.Position);
    }

    [Fact]
    public async Task Open_Should_SkipMembers_When_AncestorIsExcluded()
    {
        // arrange
        // act
        var scope = await OpenAsync(
            """
            query ($skip: Boolean!) {
                reversed {
                    pageInfo @skip(if: $skip) { hasNextPage }
                    nodes { name }
                    edges
                }
            }
            """,
            skip: true);

        // assert
        Assert.Equal(1, scope!.Position);
    }

    [Fact]
    public async Task Release_Should_MoveToTheNextSlot_When_TheCurrentSlotIsReleased()
    {
        // arrange
        // act
        var positions = await RunWithScopeAsync(
            Query,
            scope =>
            {
                var positions = new List<int> { scope.Position };
                scope.Release(0);
                positions.Add(scope.Position);
                scope.Release(1);
                positions.Add(scope.Position);
                scope.Release(2);
                positions.Add(scope.Position);
                return positions;
            });

        // assert
        positions.Select(p => p.ToString()).MatchInlineSnapshots(["0", "1", "2", "3"]);
    }

    [Fact]
    public async Task Release_Should_KeepThePosition_When_ALaterSlotIsReleasedBeforeItsTurn()
    {
        // arrange
        // act
        var positions = await RunWithScopeAsync(
            Query,
            scope =>
            {
                var positions = new List<int>();
                scope.Release(2);
                positions.Add(scope.Position);
                scope.Release(0);
                positions.Add(scope.Position);
                scope.Release(1);
                positions.Add(scope.Position);
                return positions;
            });

        // assert
        positions.Select(p => p.ToString()).MatchInlineSnapshots(["0", "1", "3"]);
    }

    [Fact]
    public async Task Release_Should_BeIdempotent_When_SlotIsReleasedTwice()
    {
        // arrange
        // act
        var position = await RunWithScopeAsync(
            Query,
            scope =>
            {
                scope.Release(0);
                scope.Release(0);
                return scope.Position;
            });

        // assert
        Assert.Equal(1, position);
    }

    [Fact]
    public async Task Release_Should_RunStoredMemberOnce_When_ItsSlotIsReleasedBeforeItsTurn()
    {
        // arrange
        // act
        var states = await ExecutionGroupTestHarness.RunAsync(
            Query,
            harness =>
            {
                var scheduler = harness.Scheduler;
                var scope = scheduler.OpenExecutionGroupScope(harness.Plan, harness.IncludeFlags)!;
                var first = CreateMember(harness, scope, "nodes");
                var second = CreateMember(harness, scope, "edges");
                var states = new List<string>();

                scheduler.Register(first);
                scheduler.Register(second);
                states.Add(Describe(first, second));

                scope.Release(second.GroupSlot);
                states.Add(Describe(first, second));
                var idAfterRelease = second.Id;

                scope.Release(first.GroupSlot);
                states.Add(Describe(first, second));
                states.Add($"second id unchanged: {second.Id == idAfterRelease}");
                return states;
            });

        // assert
        states.MatchInlineSnapshots(
        [
            "first: registered, second: not registered",
            "first: registered, second: registered",
            "first: registered, second: registered",
            "second id unchanged: True"
        ]);
    }

    [Fact]
    public async Task ReleaseSlotsBelow_Should_SkipMembersBelowTheSelection_When_NoChildTaskWasCreated()
    {
        // arrange
        // act
        var position = await RunWithScopeAsync(
            Query,
            (harness, scope) =>
            {
                scope.Release(0);
                scope.Release(1);
                scope.ReleaseSlotsBelow(harness.GetSelection("pageInfo"));
                return scope.Position;
            });

        // assert
        Assert.Equal(3, position);
    }

    [Fact]
    public async Task Abandon_Should_MoveBehindTheLastSlot_When_ScopeIsAbandoned()
    {
        // arrange
        // act
        var position = await RunWithScopeAsync(
            Query,
            scope =>
            {
                scope.Abandon();
                return scope.Position;
            });

        // assert
        Assert.Equal(3, position);
    }

    [Fact]
    public async Task Release_Should_AdvanceAcrossWords_When_ScopeHasMoreThan64Slots()
    {
        // arrange
        var builder = new StringBuilder("{ connection {");

        for (var i = 0; i < 70; i++)
        {
            builder.Append($" a{i}: nodes {{ name }}");
        }

        builder.Append(" } }");

        // act
        var positions = await RunWithScopeAsync(
            builder.ToString(),
            scope =>
            {
                var positions = new List<int>();
                scope.Release(65);

                for (var i = 0; i < 64; i++)
                {
                    scope.Release(i);
                }

                positions.Add(scope.Position);
                scope.Release(64);
                positions.Add(scope.Position);

                for (var i = 66; i < 70; i++)
                {
                    scope.Release(i);
                }

                positions.Add(scope.Position);
                return positions;
            });

        // assert
        positions.Select(p => p.ToString()).MatchInlineSnapshots(["64", "66", "70"]);
    }

    private static Task<ExecutionGroupScope?> OpenAsync(string query)
        => ExecutionGroupTestHarness.RunAsync(
            query,
            harness => harness.Scheduler.OpenExecutionGroupScope(harness.Plan, harness.IncludeFlags));

    private static Task<ExecutionGroupScope?> OpenAsync(string query, bool skip)
        => ExecutionGroupTestHarness.RunAsync(
            query,
            new Dictionary<string, object?> { ["skip"] = skip },
            harness => harness.Scheduler.OpenExecutionGroupScope(harness.Plan, harness.IncludeFlags));

    private static Task<T> RunWithScopeAsync<T>(string query, Func<ExecutionGroupScope, T> test)
        => RunWithScopeAsync(query, (_, scope) => test(scope));

    private static Task<T> RunWithScopeAsync<T>(
        string query,
        Func<ExecutionGroupTestHarness, ExecutionGroupScope, T> test)
        => ExecutionGroupTestHarness.RunAsync(
            query,
            harness =>
            {
                var scope = harness.Scheduler.OpenExecutionGroupScope(harness.Plan, harness.IncludeFlags)!;
                return test(harness, scope);
            });

    private static ResolverTask CreateMember(
        ExecutionGroupTestHarness harness,
        ExecutionGroupScope scope,
        string fieldName)
    {
        var member = harness.OperationContext.CreateResolverTask(
            parent: null,
            harness.GetSelection(fieldName),
            default,
            ImmutableDictionary<string, object?>.Empty);

        member.JoinExecutionGroup(scope);
        return member;
    }

    private static string Describe(ResolverTask first, ResolverTask second)
        => $"first: {Describe(first)}, second: {Describe(second)}";

    private static string Describe(ResolverTask task)
        => task.IsRegistered ? "registered" : "not registered";
}
