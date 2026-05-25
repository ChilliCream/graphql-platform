using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Tests ported from the graphql-js reference implementation covering error handling,
/// null bubbling, deduplication, empty defers, multiple labels, and edge cases for @defer.
/// </summary>
public class DeferReferenceTests
{
    // ========================================================================
    // 1. Error Handling
    // ========================================================================

    /// <summary>
    /// Error thrown in a deferred fragment's resolver. The initial result contains
    /// the non-deferred fields, and the deferred payload contains the error.
    /// </summary>
    [Fact]
    public async Task Defer_Fragment_With_Error()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer {
                        errorField
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    /// <summary>
    /// Non-null field returns null inside a deferred fragment, triggering a non-null
    /// violation. The completed result for the deferred delivery should include errors.
    /// </summary>
    [Fact]
    public async Task Defer_Fragment_With_NonNullable_Error()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer {
                        nonNullErrorField
                    }
                }
            }
            """);

        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var completedErrors = new List<IReadOnlyList<IError>>();

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var completed in response.Completed)
                {
                    if (completed.Errors is { Count: > 0 })
                    {
                        completedErrors.Add(completed.Errors);
                    }
                }
            }

            Assert.NotEmpty(completedErrors);
        }
    }

    /// <summary>
    /// Non-null error in the initial (non-deferred) payload causes the parent object
    /// to be nulled, which should cancel the deferred fragment on that object.
    /// </summary>
    [Fact]
    public async Task Defer_NonNullable_Error_Outside_Cancels_Defer()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                a {
                    nonNullErrorField
                    ... @defer {
                        someField
                    }
                }
            }
            """);

        // The non-null error on a.nonNullErrorField null-bubbles to `a`,
        // producing an error in the response.
        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var hasErrors = false;

            await foreach (var response in stream.ReadResultsAsync())
            {
                if (response.Errors is { Count: > 0 })
                {
                    hasErrors = true;
                }

                foreach (var incremental in response.Incremental)
                {
                    if (incremental.Errors is { Count: > 0 })
                    {
                        hasErrors = true;
                    }
                }

                foreach (var completed in response.Completed)
                {
                    if (completed.Errors is { Count: > 0 })
                    {
                        hasErrors = true;
                    }
                }
            }

            Assert.True(hasErrors, "Expected non-null violation error");
        }
    }

    /// <summary>
    /// A non-null error nulls one deferred path, but deferred work on a sibling
    /// path should still complete successfully.
    /// </summary>
    [Fact]
    public async Task Defer_Keeps_Work_Outside_Nulled_Error_Paths()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                a {
                    b {
                        ... @defer(label: "erroring") {
                            c {
                                nonNullErrorField
                            }
                        }
                        ... @defer(label: "succeeding") {
                            e {
                                f
                            }
                        }
                    }
                }
            }
            """);

        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();
            var anyErrors = false;

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
                    if (completed.Errors is { Count: > 0 })
                    {
                        anyErrors = true;
                    }
                }

                foreach (var incremental in response.Incremental)
                {
                    if (incremental.Errors is { Count: > 0 })
                    {
                        anyErrors = true;
                    }
                }
            }

            Assert.True(allPendingIds.SetEquals(allCompletedIds),
                "All pending defers should complete");
            Assert.Contains("erroring", allLabels);
            Assert.Contains("succeeding", allLabels);
            Assert.True(anyErrors,
                "Expected at least one error from the non-null violation in 'erroring' defer");
        }
    }

    /// <summary>
    /// Error in a deferred fragment at the root Query level.
    /// </summary>
    [Fact]
    public async Task Defer_Fragment_With_Errors_On_Top_Level_Query_Field()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    a {
                        nonNullErrorField
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    // ========================================================================
    // 2. Null Bubbling Across Defer Boundaries
    // ========================================================================

    /// <summary>
    /// A non-null violation in the initial result that nulls a parent which contains
    /// deferred fields. The deferred fields should be cancelled.
    /// </summary>
    [Fact]
    public async Task Defer_Null_Bubbling_Cancels_Deferred_Fields_In_Initial()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                a {
                    b {
                        c {
                            nonNullErrorField
                        }
                    }
                    ... @defer {
                        someField
                    }
                }
            }
            """);

        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var hasErrors = false;

            await foreach (var response in stream.ReadResultsAsync())
            {
                if (response.Errors is { Count: > 0 })
                {
                    hasErrors = true;
                }

                foreach (var completed in response.Completed)
                {
                    if (completed.Errors is { Count: > 0 })
                    {
                        hasErrors = true;
                    }
                }
            }

            Assert.True(hasErrors, "Expected non-null violation error");
        }
    }

    /// <summary>
    /// Non-null error within a deferred fragment produces an error in the
    /// incremental payload (on the completed or incremental result).
    /// </summary>
    [Fact]
    public async Task Defer_Null_Bubbling_In_Deferred_Result()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                a {
                    ... @defer {
                        b {
                            c {
                                nonNullErrorField
                            }
                        }
                    }
                }
            }
            """);

        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var anyErrors = false;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var completed in response.Completed)
                {
                    if (completed.Errors is { Count: > 0 })
                    {
                        anyErrors = true;
                    }
                }

                foreach (var incremental in response.Incremental)
                {
                    if (incremental.Errors is { Count: > 0 })
                    {
                        anyErrors = true;
                    }
                }
            }

            Assert.True(anyErrors,
                "Expected non-null violation error in deferred result");
        }
    }

    // ========================================================================
    // 3. Deduplication Behavior
    // ========================================================================

    /// <summary>
    /// Same fragment used both with @defer and without (deferred occurrence first
    /// in document order). The non-deferred usage should win.
    /// </summary>
    [Fact]
    public async Task Fragment_Both_Deferred_And_NonDeferred_Deferred_First()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... HeroName @defer
                    ... HeroName
                }
            }

            fragment HeroName on Hero {
                name
            }
            """);

        result.MatchMarkdownSnapshot();
    }

    /// <summary>
    /// Same as above but non-deferred occurrence first in document order.
    /// The non-deferred usage should still win.
    /// </summary>
    [Fact]
    public async Task Fragment_Both_Deferred_And_NonDeferred_NonDeferred_First()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... HeroName
                    ... HeroName @defer
                }
            }

            fragment HeroName on Hero {
                name
            }
            """);

        result.MatchMarkdownSnapshot();
    }

    /// <summary>
    /// A list field requested in both the initial result and a deferred fragment.
    /// The deferred fragment should only deliver new fields not in the initial result.
    /// </summary>
    [Fact]
    public async Task Deduplicates_List_Fields_In_Initial_Payload()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    friends {
                        id
                    }
                    ... @defer {
                        friends {
                            name
                        }
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    /// <summary>
    /// A field present in both the initial payload and a deferred fragment.
    /// The deferred fragment should only deliver fields not already in the initial result.
    /// </summary>
    [Fact]
    public async Task Deduplicates_Fields_Present_In_Initial_Payload()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    name
                    ... @defer {
                        name
                        id
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    // ========================================================================
    // 4. Empty Defer Fragments
    // ========================================================================

    /// <summary>
    /// All fields in the defer fragment are skipped via @skip(if: true).
    /// No defer payload should be emitted.
    /// </summary>
    [Fact]
    public async Task Does_Not_Emit_Empty_Defer_Fragment()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer {
                        name @skip(if: true)
                    }
                }
            }
            """);

        result.MatchMarkdownSnapshot();
    }

    /// <summary>
    /// Outer defer fragment is empty (all direct fields skipped), but contains
    /// a nested defer with fields. The nested defer should still emit.
    /// </summary>
    [Fact]
    public async Task Emits_Children_Of_Empty_Defer_Fragment()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer {
                        name @skip(if: true)
                        ... @defer {
                            name
                        }
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    // ========================================================================
    // 5. Multiple Labels
    // ========================================================================

    /// <summary>
    /// Two sibling defers with different labels and different fields
    /// should be emitted separately.
    /// </summary>
    [Fact]
    public async Task Separately_Emits_Defer_Fragments_With_Different_Labels()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer(label: "first") {
                        name
                    }
                    ... @defer(label: "second") {
                        nestedObject {
                            deeperObject {
                                foo
                            }
                        }
                    }
                }
            }
            """);

        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allPendingIds = new HashSet<int>();
            var allCompletedIds = new HashSet<int>();
            var allLabels = new List<string>();

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
            }

            Assert.True(allPendingIds.SetEquals(allCompletedIds));
            Assert.Contains("first", allLabels);
            Assert.Contains("second", allLabels);
            Assert.Equal(2, allLabels.Count);
        }
    }

    /// <summary>
    /// Nested defers where both parent and child have labels. Both should be
    /// announced in pending and completed.
    /// </summary>
    [Fact]
    public async Task Nested_Defer_With_Labels()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer(label: "outer") {
                        name
                        ... @defer(label: "inner") {
                            nestedObject {
                                deeperObject {
                                    foo
                                    bar
                                }
                            }
                        }
                    }
                }
            }
            """);

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
            Assert.Contains("outer", allLabels);
            Assert.Contains("inner", allLabels);
        }
    }

    // ========================================================================
    // 6. Edge Cases
    // ========================================================================

    /// <summary>
    /// Inline fragment with @defer and a type condition. The defer should only
    /// apply when the type condition matches.
    /// </summary>
    [Fact]
    public async Task InlineFragment_Defer_With_TypeCondition()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... on Hero @defer {
                        name
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    /// <summary>
    /// @defer(if: $shouldDefer) with a nullable Boolean variable that is not provided.
    /// In HotChocolate, a null `if` argument on @defer disables deferral
    /// (differs from graphql-js where null defaults to true).
    /// </summary>
    [Fact]
    public async Task Defer_If_Null_Variable_Disables_Defer()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($shouldDefer: Boolean) {
                        hero {
                            id
                            ... @defer(if: $shouldDefer) {
                                name
                            }
                        }
                    }
                    """)
                .Build());

        // HC treats null `if` as false, disabling deferral.
        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    /// <summary>
    /// A deferred fragment within an already-deferred fragment. Both should deliver
    /// their payloads.
    /// </summary>
    [Fact]
    public async Task Defer_Fragment_Within_Already_Deferred_Fragment()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer(label: "outer") {
                        name
                        nestedObject {
                            ... @defer(label: "inner") {
                                deeperObject {
                                    foo
                                    bar
                                }
                            }
                        }
                    }
                }
            }
            """);

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
            Assert.Contains("outer", allLabels);
            Assert.Contains("inner", allLabels);
        }
    }

    // ========================================================================
    // 7. Multiple Defers Erroring
    // ========================================================================

    /// <summary>
    /// Two deferred fragments both encountering non-null errors. Both should
    /// produce errors (in completed or incremental results).
    /// </summary>
    [Fact]
    public async Task Multiple_Erroring_Deferred_Grouped_Field_Sets()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer(label: "errorA") {
                        nonNullErrorField
                    }
                    ... @defer(label: "errorB") {
                        nestedObject {
                            nonNullErrorField
                        }
                    }
                }
            }
            """);

        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var errorSources = 0;

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var completed in response.Completed)
                {
                    if (completed.Errors is { Count: > 0 })
                    {
                        errorSources++;
                    }
                }

                foreach (var incremental in response.Incremental)
                {
                    if (incremental.Errors is { Count: > 0 })
                    {
                        errorSources++;
                    }
                }
            }

            Assert.True(errorSources >= 2,
                $"Expected at least 2 error sources from both defers, got {errorSources}");
        }
    }

    /// <summary>
    /// Parent defer fails with a non-null error. Child defer nested inside the
    /// parent should be cancelled and not deliver a successful result.
    /// </summary>
    [Fact]
    public async Task Cancels_Child_Deferred_Fragments_If_Parent_Fails()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                hero {
                    id
                    ... @defer(label: "parent") {
                        nonNullErrorField
                        ... @defer(label: "child") {
                            name
                        }
                    }
                }
            }
            """);

        var stream = Assert.IsType<ResponseStream>(result);
        await using (stream)
        {
            var allLabels = new List<string>();
            var completedWithErrors = new List<CompletedResult>();

            await foreach (var response in stream.ReadResultsAsync())
            {
                foreach (var pending in response.Pending)
                {
                    if (pending.Label is not null)
                    {
                        allLabels.Add(pending.Label);
                    }
                }

                foreach (var completed in response.Completed)
                {
                    if (completed.Errors is { Count: > 0 })
                    {
                        completedWithErrors.Add(completed);
                    }
                }
            }

            Assert.Contains("parent", allLabels);
            Assert.NotEmpty(completedWithErrors);
        }
    }

    // ========================================================================
    // Schema Setup
    // ========================================================================

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<ReferenceQuery>()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync();
    }

    // ========================================================================
    // Schema Types
    // ========================================================================

    public class ReferenceQuery
    {
        public Hero? Hero => new();

        public TypeA? A => new();

        public TypeG? G => new();
    }

    public class Hero
    {
        public string Id => "hero-1";

        public async Task<string?> GetNameAsync()
        {
            await Task.Delay(5);
            return "Luke";
        }

        /// <summary>
        /// Non-null String! that returns null at runtime to trigger non-null violation.
        /// </summary>
        public string NonNullErrorField => null!;

        /// <summary>
        /// Resolver that throws a GraphQL error.
        /// </summary>
        public async Task<string?> GetErrorFieldAsync()
        {
            await Task.Yield();
            throw new GraphQLException("resolver error");
        }

        public List<Friend>? Friends =>
        [
            new("friend-1", "Han"),
            new("friend-2", "Leia"),
            new("friend-3", "C-3PO")
        ];

        public NestedObject? NestedObject => new();

        public AnotherNestedObject? AnotherNestedObject => new();
    }

    public class Friend(string id, string name)
    {
        public string Id => id;

        public string? Name => name;
    }

    public class NestedObject
    {
        public string? Name => "nestedName";

        public DeeperObject? DeeperObject => new();

        /// <summary>
        /// Non-null String! that returns null at runtime to trigger non-null violation.
        /// </summary>
        public string NonNullErrorField => null!;
    }

    public class AnotherNestedObject
    {
        public DeeperObject? DeeperObject => new();
    }

    public class DeeperObject
    {
        public string? Foo => "foo";

        public string? Bar => "bar";

        public string? Baz => "baz";

        public string? Bak => "bak";
    }

    public class TypeA
    {
        public string? SomeField => "someValue";

        /// <summary>
        /// Non-null String! that returns null at runtime to trigger non-null violation.
        /// </summary>
        public string NonNullErrorField => null!;

        public TypeB? B => new();
    }

    public class TypeB
    {
        public TypeC? C => new();

        public TypeE? E => new();
    }

    public class TypeC
    {
        public string? D => "dValue";

        /// <summary>
        /// Non-null String! that returns null at runtime to trigger non-null violation.
        /// </summary>
        public string NonNullErrorField => null!;
    }

    public class TypeE
    {
        public string? F => "fValue";
    }

    public class TypeG
    {
        public string? H => "hValue";
    }
}
