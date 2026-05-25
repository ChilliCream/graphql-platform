using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Integration tests verifying correct @defer behavior with sibling deferred fragments
/// that overlap on the same selection paths. These tests specifically target the bug where
/// sibling defers caused an explosion of pending IDs and incomplete streams.
/// </summary>
public class DeferSiblingTests
{
    /// <summary>
    /// Three sibling @defer fragments on the same type where two fragments overlap on
    /// a nested field path. This mimics the production query structure that caused the bug.
    /// Verifies: exactly 3 pending IDs, all completed, hasNext:false at stream end.
    /// </summary>
    [Fact]
    public async Task Three_Sibling_Defers_With_Overlapping_Paths()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                stage {
                    id
                    ... FragmentA @defer(label: "a")
                    ... FragmentB @defer(label: "b")
                    ... FragmentC @defer(label: "c")
                }
            }

            fragment FragmentA on Stage {
                metrics {
                    operations {
                        summary
                    }
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    name
                                }
                            }
                        }
                    }
                }
            }

            fragment FragmentB on Stage {
                displayName
                version
            }

            fragment FragmentC on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    name
                                    impact
                                    latency
                                    throughput
                                }
                                cursor
                            }
                            totalCount
                        }
                    }
                }
            }
            """);

        // assert
        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();
            var lastHasNext = true;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var pending in response.Pending)
                {
                    allPendingIds.Add(pending.Id);
                    if (pending.Label is not null)
                    {
                        allLabels.Add(pending.Label);
                    }
                }

                foreach (var completed in response.Completed)
                {
                    allCompletedIds.Add(completed.Id);
                }

                lastHasNext = response.HasNext ?? false;
            }

            Assert.False(lastHasNext, "Stream should end with hasNext: false");
            Assert.Equal(3, allPendingIds.Count);
            Assert.True(
                allPendingIds.SetEquals(allCompletedIds),
                $"Pending IDs {string.Join(",", allPendingIds)} != Completed IDs {string.Join(",", allCompletedIds)}");

            // Each label must appear exactly once (no duplicate pending announcements)
            Assert.Equal(allLabels.Count, new HashSet<string>(allLabels).Count);
            Assert.Contains("a", allLabels);
            Assert.Contains("b", allLabels);
            Assert.Contains("c", allLabels);
        }
    }

    /// <summary>
    /// Two sibling @defer fragments selecting the exact same field with different
    /// sub-selections. This is the simplest overlap case.
    /// </summary>
    [Fact]
    public async Task Two_Sibling_Defers_Same_Root_Field_Different_Subselections()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                stage {
                    id
                    ... FragA @defer(label: "a")
                    ... FragB @defer(label: "b")
                }
            }

            fragment FragA on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    name
                                }
                            }
                        }
                    }
                }
            }

            fragment FragB on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    impact
                                    throughput
                                }
                            }
                            totalCount
                        }
                    }
                }
            }
            """);

        // assert
        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();
            var lastHasNext = true;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var pending in response.Pending)
                {
                    allPendingIds.Add(pending.Id);
                    if (pending.Label is not null)
                    {
                        allLabels.Add(pending.Label);
                    }
                }

                foreach (var completed in response.Completed)
                {
                    allCompletedIds.Add(completed.Id);
                }

                lastHasNext = response.HasNext ?? false;
            }

            Assert.False(lastHasNext, "Stream should end with hasNext: false");
            Assert.Equal(2, allPendingIds.Count);
            Assert.True(allPendingIds.SetEquals(allCompletedIds));

            // Each label must appear exactly once
            Assert.Equal(allLabels.Count, new HashSet<string>(allLabels).Count);
            Assert.Contains("a", allLabels);
            Assert.Contains("b", allLabels);
        }
    }

    /// <summary>
    /// Three sibling defers where ALL three overlap on the same deeply nested path.
    /// Stresses the set-based grouping — the field has DeferUsageSet {a, b, c}.
    /// </summary>
    [Fact]
    public async Task Three_Sibling_Defers_All_Overlapping_On_Same_Field()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                stage {
                    id
                    ... FragA @defer(label: "a")
                    ... FragB @defer(label: "b")
                    ... FragC @defer(label: "c")
                }
            }

            fragment FragA on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    name
                                }
                            }
                        }
                    }
                }
            }

            fragment FragB on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    latency
                                }
                            }
                        }
                    }
                }
            }

            fragment FragC on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    throughput
                                }
                            }
                        }
                    }
                }
            }
            """);

        // assert
        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();
            var lastHasNext = true;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var pending in response.Pending)
                {
                    allPendingIds.Add(pending.Id);
                    if (pending.Label is not null)
                    {
                        allLabels.Add(pending.Label);
                    }
                }

                foreach (var completed in response.Completed)
                {
                    allCompletedIds.Add(completed.Id);
                }

                lastHasNext = response.HasNext ?? false;
            }

            Assert.False(lastHasNext, "Stream should end with hasNext: false");
            Assert.Equal(3, allPendingIds.Count);
            Assert.True(allPendingIds.SetEquals(allCompletedIds));

            // Each label must appear exactly once
            Assert.Equal(allLabels.Count, new HashSet<string>(allLabels).Count);
            Assert.Contains("a", allLabels);
            Assert.Contains("b", allLabels);
            Assert.Contains("c", allLabels);
        }
    }

    /// <summary>
    /// Sibling defers with a list field — ensures the fix doesn't create per-list-item
    /// branches for sibling defers (the core of the production bug).
    /// </summary>
    [Fact]
    public async Task Sibling_Defers_Over_List_Field_Do_Not_Explode_Pending_IDs()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                stage {
                    id
                    ... FragA @defer(label: "a")
                    ... FragC @defer(label: "c")
                }
            }

            fragment FragA on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    name
                                }
                            }
                        }
                    }
                }
            }

            fragment FragC on Stage {
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    name
                                    impact
                                    latency
                                    throughput
                                }
                                cursor
                            }
                            totalCount
                        }
                    }
                }
            }
            """);

        // assert
        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();
            var lastHasNext = true;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var pending in response.Pending)
                {
                    allPendingIds.Add(pending.Id);
                    if (pending.Label is not null)
                    {
                        allLabels.Add(pending.Label);
                    }
                }

                foreach (var completed in response.Completed)
                {
                    allCompletedIds.Add(completed.Id);
                }

                lastHasNext = response.HasNext ?? false;
            }

            Assert.False(lastHasNext, "Stream should end with hasNext: false");

            // Must be exactly 2 pending IDs (one per @defer), NOT N * listLength
            Assert.Equal(2, allPendingIds.Count);
            Assert.True(allPendingIds.SetEquals(allCompletedIds));

            // Each label must appear exactly once
            Assert.Equal(allLabels.Count, new HashSet<string>(allLabels).Count);
            Assert.Contains("a", allLabels);
            Assert.Contains("c", allLabels);
        }
    }

    /// <summary>
    /// Nested @defer inside a sibling @defer — verifies that the fix correctly handles
    /// nested defers (child of parent) while skipping sibling defers.
    /// </summary>
    [Fact]
    public async Task Sibling_Defer_With_Nested_Defer_Inside()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                stage {
                    id
                    ... FragA @defer(label: "a")
                    ... FragB @defer(label: "b")
                }
            }

            fragment FragA on Stage {
                displayName
                metrics {
                    subgraphs {
                        insights {
                            edges {
                                node {
                                    id
                                    ... @defer(label: "a_details") {
                                        latency
                                        throughput
                                    }
                                }
                            }
                        }
                    }
                }
            }

            fragment FragB on Stage {
                version
            }
            """);

        // assert
        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();
            var lastHasNext = true;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var pending in response.Pending)
                {
                    allPendingIds.Add(pending.Id);
                    if (pending.Label is not null)
                    {
                        allLabels.Add(pending.Label);
                    }
                }

                foreach (var completed in response.Completed)
                {
                    allCompletedIds.Add(completed.Id);
                }

                lastHasNext = response.HasNext ?? false;
            }

            Assert.False(lastHasNext, "Stream should end with hasNext: false");
            Assert.True(allPendingIds.SetEquals(allCompletedIds));

            // Top-level sibling labels must appear exactly once
            Assert.Equal(1, allLabels.Count(l => l == "a"));
            Assert.Equal(1, allLabels.Count(l => l == "b"));

            // Nested defer "a_details" appears once per list item (3 edges)
            Assert.Contains("a_details", allLabels);
        }
    }

    /// <summary>
    /// Single @defer — basic regression test that single defers still work.
    /// </summary>
    [Fact]
    public async Task Single_Defer_Still_Works()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                stage {
                    id
                    ... @defer(label: "details") {
                        displayName
                        version
                    }
                }
            }
            """);

        // assert
        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();
            var lastHasNext = true;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var pending in response.Pending)
                {
                    allPendingIds.Add(pending.Id);
                    if (pending.Label is not null)
                    {
                        allLabels.Add(pending.Label);
                    }
                }

                foreach (var completed in response.Completed)
                {
                    allCompletedIds.Add(completed.Id);
                }

                lastHasNext = response.HasNext ?? false;
            }

            Assert.False(lastHasNext, "Stream should end with hasNext: false");
            Assert.Single(allPendingIds);
            Assert.True(allPendingIds.SetEquals(allCompletedIds));

            // Each label must appear exactly once
            Assert.Equal(allLabels.Count, new HashSet<string>(allLabels).Count);
            Assert.Contains("details", allLabels);
        }
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<StageQuery>()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync();
    }

    // Schema types that model the production query structure

    public class StageQuery
    {
        public async Task<Stage> GetStageAsync()
        {
            await Task.Delay(10);
            return new Stage();
        }
    }

    public class Stage
    {
        public string Id => "stage-1";
        public string DisplayName => "My Stage";
        public string Version => "1.0";

        public async Task<StageMetrics> GetMetricsAsync()
        {
            await Task.Delay(10);
            return new StageMetrics();
        }
    }

    public class StageMetrics
    {
        public async Task<OperationMetrics> GetOperationsAsync()
        {
            await Task.Delay(10);
            return new OperationMetrics();
        }

        public async Task<SubgraphMetrics> GetSubgraphsAsync()
        {
            await Task.Delay(10);
            return new SubgraphMetrics();
        }
    }

    public class OperationMetrics
    {
        public string Summary => "ops-summary";
    }

    public class SubgraphMetrics
    {
        public async Task<SubgraphInsightsConnection> GetInsightsAsync()
        {
            await Task.Delay(10);
            return new SubgraphInsightsConnection();
        }
    }

    public class SubgraphInsightsConnection
    {
        public int TotalCount => 3;

        public List<SubgraphInsightsEdge> Edges =>
        [
            new SubgraphInsightsEdge(new SubgraphInsight("sg-1", "Users", 0.8, 12.5, 100.0), "cursor-1"),
            new SubgraphInsightsEdge(new SubgraphInsight("sg-2", "Products", 0.6, 8.3, 200.0), "cursor-2"),
            new SubgraphInsightsEdge(new SubgraphInsight("sg-3", "Orders", 0.9, 15.1, 150.0), "cursor-3")
        ];
    }

    public class SubgraphInsightsEdge(SubgraphInsight node, string cursor)
    {
        public SubgraphInsight Node => node;
        public string Cursor => cursor;
    }

    public class SubgraphInsight(string id, string name, double impact, double latency, double throughput)
    {
        public string Id => id;
        public string Name => name;
        public double Impact => impact;
        public double Latency => latency;
        public double Throughput => throughput;
    }
}
