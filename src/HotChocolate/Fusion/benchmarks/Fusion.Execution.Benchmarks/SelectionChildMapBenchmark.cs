using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;
using SelectionSet = HotChocolate.Fusion.Execution.Nodes.SelectionSet;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures the per-property child selection set resolution of the merge completion
/// loops. Today ValueCompletion resolves the child <see cref="ResultSelectionSet"/>
/// by response name for every property that misses the scalar fast path:
/// <c>BuildResult</c> calls the type-unaware <c>ResultSelectionSet.TryGetChild</c>
/// at ValueCompletion.cs lines 147 and 198 (once per property per merged row) and
/// <c>TryCompleteObjectValue</c> calls the type-aware overload at lines 1183 and
/// 1227 (once per property per object element). TryGetChild (ResultSelectionSet.cs
/// lines 83-121 and 129-176) does up to 7 ordinal string compares, or a dictionary
/// probe at 8 or more selections, and on a direct miss recurses through the inline
/// fragment bodies. For a batch of N rows with k such properties the identical
/// (response name to child) answers are recomputed N times k times per merge, and
/// leaf selections always resolve to null yet pay the full lookup.
///
/// The optimized variant is the reconciled two-stage design. Stage 1 is a leaf
/// gate on the cached <c>Selection.IsLeaf</c> flag (Selection.cs line 85): leaf
/// named types never have a child set, so the lookup is skipped. Stage 2 is a
/// lazily built copy-on-write memo mapping a <see cref="SelectionSet"/> to an
/// array of child <see cref="ResultSelectionSet"/> references indexed by
/// <c>selection.Id - selectionSet.Id - 1</c>, the dense id invariant that
/// CompositeObjectContext.cs line 48 already relies on. Memo entries are built by
/// calling the existing TryGetChild once per selection, so lookup semantics are
/// identical by construction, and the type-aware map is only used when the runtime
/// object type is reference-equal to the selection set's own type (the guard that
/// excludes the @interfaceObject divergence case). In the product the memo would
/// live on the plan-lifetime ResultSelectionSet itself; the benchmark keeps it in
/// a local wrapper because product code stays unchanged.
///
/// Shapes: Small (5 direct fields, linear scan) and Large (9 direct fields,
/// dictionary) exercise the type-unaware BuildResult sites; Fragment (3 direct
/// fields plus 2 type-conditioned inline fragments) exercises the type-aware
/// TryCompleteObjectValue site, where fragment-nested fields miss the direct scan
/// and pay the recursive fragment walk today. Rows models the merged rows per
/// batch (1 is the single-object regression shape). Every property is modeled as
/// missing the scalar fast path, matching null-heavy or error-carrying rows; fast
/// path properties never reach this lookup in either variant. The cold variant
/// rebuilds the memo per invocation to expose the first-use build cost.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class SelectionChildMapBenchmark : FusionBenchmarkBase
{
    /// <summary>
    /// BenchmarkDotNet 0.15.8 has no RuntimeMoniker for the net11.0 preview host and
    /// this project pins TargetFramework to net11.0, so out-of-process toolchains can
    /// neither validate nor build a child process here. The job therefore runs in
    /// process with the intended 3 warmup and 10 measurement iterations.
    /// </summary>
    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
            => AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithToolchain(InProcessEmitToolchain.Instance));
    }

    private const string OperationId = "123456789101112";

    // The composite operation. Its compiled SelectionSets provide the real
    // Selection/SelectionSet ids that feed the indexed map math.
    private const string QuerySource =
        """
        {
          productById(id: "1") {
            id
            name
            description
            price
            dimension { height width }
          }
          products(first: 10) {
            nodes {
              id
              name
              description
              price
              averageRating
              title
              estimatedDelivery
              dimension { height width }
              reviews { nodes { id body stars } }
            }
          }
          searchContent(query: "benchmark") {
            id
            title
            description
            ... on Product {
              name
              price
              dimension { height width }
            }
            ... on Article {
              content
              publishedAt
              author { id displayName }
            }
          }
        }
        """;

    // 5 direct fields, no fragments: a linear-scan ResultSelectionSet, matching
    // the productById selection set of the operation. Type-unaware BuildResult shape.
    private const string SmallSource =
        """
        {
          id
          name
          description
          price
          dimension { height width }
        }
        """;

    // 9 direct fields, no fragments: a dictionary ResultSelectionSet, matching the
    // products.nodes selection set of the operation. Type-unaware BuildResult shape.
    private const string LargeSource =
        """
        {
          id
          name
          description
          price
          averageRating
          title
          estimatedDelivery
          dimension { height width }
          reviews { nodes { id body stars } }
        }
        """;

    // 3 direct fields plus 2 type-conditioned inline fragments, matching the
    // searchContent selection set of the operation compiled for the Product type
    // context. Type-aware TryCompleteObjectValue shape: name, price and dimension
    // miss the direct scan and resolve through the fragment walk today.
    private const string FragmentSource =
        """
        {
          id
          title
          description
          ... on Product {
            name
            price
            dimension { height width }
          }
          ... on Article {
            content
            publishedAt
            author { id displayName }
          }
        }
        """;

    [Params("Small", "Large", "Fragment")]
    public string Shape = "Small";

    [Params(1, 100)]
    public int Rows;

    private Workload[] _workloads = null!;
    private Workload _current = null!;

    public int Consumed;

    [GlobalSetup]
    public void Setup()
    {
        var schema = CreateFusionSchema();

        if (!schema.Types.TryGetType<IObjectTypeDefinition>("Product", out var productType))
        {
            throw new InvalidOperationException("Product type not found in the composed schema.");
        }

        var documentRewriter = new DocumentRewriter(schema);
        var operationDefinition = documentRewriter
            .RewriteDocument(Utf8GraphQLParser.Parse(QuerySource))
            .GetOperation(operationName: null);
        var compiler = new OperationCompiler(
            schema,
            new NoOpObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operation = compiler.Compile(OperationId, OperationId, operationDefinition);

        var root = operation.RootSelectionSet;
        var smallSet = GetChildSet(root, "productById");
        var largeSet = GetChildSet(GetChildSet(root, "products"), "nodes");

        if (!root.TryGetSelection("searchContent", out var searchSelection))
        {
            throw new InvalidOperationException("searchContent selection not found.");
        }

        var fragmentSet = searchSelection.GetSelectionSet(productType)
            ?? throw new InvalidOperationException(
                "searchContent has no selection set for the Product type context.");

        // The type-aware map is keyed by the selection set and built for its own
        // type context, so the workload's runtime type must be the exact instance
        // the set was compiled for; otherwise the optimized variant would silently
        // measure the fallback path.
        if (!ReferenceEquals(fragmentSet.Type, productType))
        {
            throw new InvalidOperationException(
                "The Product-context selection set does not reference the schema's "
                + "Product type instance.");
        }

        // Guard the fragment workload against rewriter changes silently dropping
        // the fragment-nested selections that make this shape expensive today.
        RequireSelection(fragmentSet, "id");
        RequireSelection(fragmentSet, "title");
        RequireSelection(fragmentSet, "description");
        RequireSelection(fragmentSet, "name");
        RequireSelection(fragmentSet, "price");
        RequireSelection(fragmentSet, "dimension");

        var smallResultSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Syntax.ParseSelectionSet(SmallSource),
            schema);
        var largeResultSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Syntax.ParseSelectionSet(LargeSource),
            schema);
        var fragmentResultSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Syntax.ParseSelectionSet(FragmentSource),
            schema);

        // The candidate claim is per lookup strategy, so the shapes must keep
        // mapping to the expected strategies.
        if (smallResultSet.UsesDictionaryLookup)
        {
            throw new InvalidOperationException("The 5-field shape no longer uses a linear scan.");
        }

        if (!largeResultSet.UsesDictionaryLookup)
        {
            throw new InvalidOperationException(
                "The 9-field shape no longer uses a dictionary lookup.");
        }

        if (fragmentResultSet.UsesDictionaryLookup)
        {
            throw new InvalidOperationException(
                "The fragment shape no longer uses a linear scan for its direct fields.");
        }

        _workloads =
        [
            new Workload
            {
                Name = "Small",
                ResultSet = smallResultSet,
                SelectionSet = smallSet,
                ObjectType = null,
                WarmCache = new ChildMapCache(smallResultSet),
                ExpectedHitsPerRow = 1
            },
            new Workload
            {
                Name = "Large",
                ResultSet = largeResultSet,
                SelectionSet = largeSet,
                ObjectType = null,
                WarmCache = new ChildMapCache(largeResultSet),
                ExpectedHitsPerRow = 2
            },
            new Workload
            {
                Name = "Fragment",
                ResultSet = fragmentResultSet,
                SelectionSet = fragmentSet,
                ObjectType = productType,
                WarmCache = new ChildMapCache(fragmentResultSet),
                ExpectedHitsPerRow = 1
            }
        ];

        VerifyEquivalence(_workloads);

        _current = Shape switch
        {
            "Small" => _workloads[0],
            "Large" => _workloads[1],
            "Fragment" => _workloads[2],
            _ => throw new InvalidOperationException($"Unknown shape '{Shape}'.")
        };
    }

    /// <summary>
    /// Current product behavior: one TryGetChild per property per row, exactly like
    /// ValueCompletion.cs lines 198 (type-unaware, Small/Large shapes) and 1227
    /// (type-aware, Fragment shape), including the lookups for leaf selections
    /// whose answer is always null.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int PerPropertyLookup_TryGetChild()
    {
        var workload = _current;
        var rows = Rows;
        var hits = 0;

        if (workload.ObjectType is { } objectType)
        {
            for (var row = 0; row < rows; row++)
            {
                hits += BaselineRowTypeAware(
                    workload.ResultSet,
                    workload.SelectionSet,
                    objectType);
            }
        }
        else
        {
            for (var row = 0; row < rows; row++)
            {
                hits += BaselineRowTypeUnaware(workload.ResultSet, workload.SelectionSet);
            }
        }

        Consumed = hits;
        return hits;
    }

    /// <summary>
    /// Candidate optimization in steady state: the leaf gate skips leaf selections
    /// entirely and the remaining properties read a prebuilt child array indexed by
    /// selection id, fetched once per row from the plan-lifetime memo.
    /// </summary>
    [Benchmark]
    public int IndexedChildMap_Warm()
    {
        var workload = _current;
        var rows = Rows;
        var hits = 0;

        if (workload.ObjectType is { } objectType)
        {
            for (var row = 0; row < rows; row++)
            {
                hits += OptimizedRowTypeAware(
                    workload.WarmCache,
                    workload.ResultSet,
                    workload.SelectionSet,
                    objectType);
            }
        }
        else
        {
            for (var row = 0; row < rows; row++)
            {
                hits += OptimizedRowTypeUnaware(workload.WarmCache, workload.SelectionSet);
            }
        }

        Consumed = hits;
        return hits;
    }

    /// <summary>
    /// First-use regression shape: a fresh memo per invocation, so the map build
    /// (one TryGetChild per selection plus the entry allocations) is paid inside
    /// the measurement. At Rows = 1 this is the worst case for the candidate; at
    /// Rows = 100 it shows the build amortizing within a single batch merge.
    /// </summary>
    [Benchmark]
    public int IndexedChildMap_ColdFirstUse()
    {
        var workload = _current;
        var cache = new ChildMapCache(workload.ResultSet);
        var rows = Rows;
        var hits = 0;

        if (workload.ObjectType is { } objectType)
        {
            for (var row = 0; row < rows; row++)
            {
                hits += OptimizedRowTypeAware(
                    cache,
                    workload.ResultSet,
                    workload.SelectionSet,
                    objectType);
            }
        }
        else
        {
            for (var row = 0; row < rows; row++)
            {
                hits += OptimizedRowTypeUnaware(cache, workload.SelectionSet);
            }
        }

        Consumed = hits;
        return hits;
    }

    /// <summary>
    /// Mirrors ValueCompletion.cs line 198: the type-unaware BuildResult site
    /// resolves the child set for every property that misses the scalar fast path.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int BaselineRowTypeUnaware(
        ResultSelectionSet resultSet,
        SelectionSet selectionSet)
    {
        var selections = selectionSet.Selections;
        var hits = 0;

        for (var i = 0; i < selections.Length; i++)
        {
            var childSet = resultSet.TryGetChild(selections[i].ResponseName);

            if (childSet is not null)
            {
                hits++;
            }
        }

        return hits;
    }

    /// <summary>
    /// Mirrors ValueCompletion.cs line 1227: the type-aware TryCompleteObjectValue
    /// site resolves the child set with the runtime object type per property.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int BaselineRowTypeAware(
        ResultSelectionSet resultSet,
        SelectionSet selectionSet,
        IComplexTypeDefinition objectType)
    {
        var selections = selectionSet.Selections;
        var hits = 0;

        for (var i = 0; i < selections.Length; i++)
        {
            var childSet = resultSet.TryGetChild(selections[i].ResponseName, objectType);

            if (childSet is not null)
            {
                hits++;
            }
        }

        return hits;
    }

    /// <summary>
    /// Optimized type-unaware row: leaf gate plus the selection-id-indexed child
    /// array, resolved lazily on the first property that needs it so all-leaf rows
    /// never pay for the map fetch.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int OptimizedRowTypeUnaware(
        ChildMapCache cache,
        SelectionSet selectionSet)
    {
        var selections = selectionSet.Selections;
        ResultSelectionSet?[]? children = null;
        var hits = 0;

        for (var i = 0; i < selections.Length; i++)
        {
            var selection = selections[i];

            // Stage 1 leaf gate: a leaf-named-type selection always maps to a
            // null child set.
            if (selection.IsLeaf)
            {
                continue;
            }

            children ??= cache.GetTypeUnawareChildren(selectionSet);

            var childSet = children[selection.Id - selectionSet.Id - 1];

            if (childSet is not null)
            {
                hits++;
            }
        }

        return hits;
    }

    /// <summary>
    /// Optimized type-aware row: same as the type-unaware variant plus the
    /// divergence guard. The memo entry is built for the selection set's own type
    /// context, so a diverging runtime type (the @interfaceObject opaque case)
    /// falls back to the per-property lookup.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int OptimizedRowTypeAware(
        ChildMapCache cache,
        ResultSelectionSet resultSet,
        SelectionSet selectionSet,
        IComplexTypeDefinition objectType)
    {
        var selections = selectionSet.Selections;
        ResultSelectionSet?[]? children = null;
        var mapResolved = false;
        var hits = 0;

        for (var i = 0; i < selections.Length; i++)
        {
            var selection = selections[i];

            if (selection.IsLeaf)
            {
                continue;
            }

            if (!mapResolved)
            {
                if (ReferenceEquals(selectionSet.Type, objectType))
                {
                    children = cache.GetTypeAwareChildren(selectionSet);
                }

                mapResolved = true;
            }

            var childSet = children is not null
                ? children[selection.Id - selectionSet.Id - 1]
                : resultSet.TryGetChild(selection.ResponseName, objectType);

            if (childSet is not null)
            {
                hits++;
            }
        }

        return hits;
    }

    private void VerifyEquivalence(Workload[] workloads)
    {
        foreach (var workload in workloads)
        {
            var set = workload.SelectionSet;
            var selections = set.Selections;

            // The indexed map relies on the dense id invariant that
            // CompositeObjectContext.cs line 48 already uses in production.
            for (var i = 0; i < selections.Length; i++)
            {
                if (selections[i].Id != set.Id + 1 + i)
                {
                    throw new InvalidOperationException(
                        $"Shape {workload.Name}: selection id {selections[i].Id} at index {i} "
                        + $"breaks the dense id invariant for selection set {set.Id}.");
                }
            }

            // Element-wise reference equality between today's TryGetChild answer
            // and the leaf gate plus indexed map answer, for every selection.
            for (var i = 0; i < selections.Length; i++)
            {
                var selection = selections[i];

                var baseline = workload.ObjectType is null
                    ? workload.ResultSet.TryGetChild(selection.ResponseName)
                    : workload.ResultSet.TryGetChild(selection.ResponseName, workload.ObjectType);

                ResultSelectionSet? optimized;

                if (selection.IsLeaf)
                {
                    optimized = null;
                }
                else
                {
                    var children = workload.ObjectType is null
                        ? workload.WarmCache.GetTypeUnawareChildren(set)
                        : workload.WarmCache.GetTypeAwareChildren(set);
                    optimized = children[selection.Id - set.Id - 1];
                }

                if (!ReferenceEquals(baseline, optimized))
                {
                    throw new InvalidOperationException(
                        $"Shape {workload.Name}: selection '{selection.ResponseName}' resolves "
                        + $"to {(baseline is null ? "null" : "a child")} via TryGetChild but "
                        + $"{(optimized is null ? "null" : "a child")} via the indexed map.");
                }
            }

            // The three row implementations must agree, and the hit count must
            // match the expected shape so a probe typo cannot silently turn the
            // workload into an all-miss loop.
            var baselineHits = workload.ObjectType is { } objectType
                ? BaselineRowTypeAware(workload.ResultSet, set, objectType)
                : BaselineRowTypeUnaware(workload.ResultSet, set);

            var warmHits = workload.ObjectType is { } warmType
                ? OptimizedRowTypeAware(workload.WarmCache, workload.ResultSet, set, warmType)
                : OptimizedRowTypeUnaware(workload.WarmCache, set);

            var coldCache = new ChildMapCache(workload.ResultSet);
            var coldHits = workload.ObjectType is { } coldType
                ? OptimizedRowTypeAware(coldCache, workload.ResultSet, set, coldType)
                : OptimizedRowTypeUnaware(coldCache, set);

            if (baselineHits != warmHits || baselineHits != coldHits)
            {
                throw new InvalidOperationException(
                    $"Shape {workload.Name}: hit counts diverge (baseline {baselineHits}, "
                    + $"warm {warmHits}, cold {coldHits}).");
            }

            if (baselineHits != workload.ExpectedHitsPerRow)
            {
                throw new InvalidOperationException(
                    $"Shape {workload.Name}: expected {workload.ExpectedHitsPerRow} non-null "
                    + $"children per row but observed {baselineHits}.");
            }
        }
    }

    private static SelectionSet GetChildSet(SelectionSet parent, string responseName)
    {
        if (!parent.TryGetSelection(responseName, out var selection))
        {
            throw new InvalidOperationException(
                $"Selection '{responseName}' not found in selection set {parent.Id}.");
        }

        return selection.GetSelectionSet()
            ?? throw new InvalidOperationException(
                $"Selection '{responseName}' has no child selection set.");
    }

    private static void RequireSelection(SelectionSet set, string responseName)
    {
        if (!set.TryGetSelection(responseName, out _))
        {
            throw new InvalidOperationException(
                $"Selection '{responseName}' not found in selection set {set.Id}; "
                + "the compiled fragment shape no longer matches the workload.");
        }
    }

    private sealed class Workload
    {
        public required string Name { get; init; }

        public required ResultSelectionSet ResultSet { get; init; }

        public required SelectionSet SelectionSet { get; init; }

        /// <summary>
        /// The runtime object type for the type-aware call sites, or null for the
        /// type-unaware BuildResult sites.
        /// </summary>
        public required IComplexTypeDefinition? ObjectType { get; init; }

        public required ChildMapCache WarmCache { get; init; }

        public required int ExpectedHitsPerRow { get; init; }
    }

    /// <summary>
    /// Benchmark-local implementation of the candidate memo. In the product these
    /// two entry arrays would be volatile fields on the plan-lifetime
    /// ResultSelectionSet (one memo per TryGetChild overload family); the wrapper
    /// exists only because the benchmark must not modify product code. Entries are
    /// matched by selection set reference, built by calling the existing
    /// TryGetChild once per selection, published copy-on-write, and capped at 8
    /// entries beyond which unseen selection sets are served without publishing.
    /// </summary>
    private sealed class ChildMapCache(ResultSelectionSet resultSelectionSet)
    {
        private const int MaxEntries = 8;

        private readonly ResultSelectionSet _resultSelectionSet = resultSelectionSet;
        private Entry[] _typeAwareEntries = [];
        private Entry[] _typeUnawareEntries = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResultSelectionSet?[] GetTypeAwareChildren(SelectionSet selectionSet)
        {
            var entries = Volatile.Read(ref _typeAwareEntries);

            for (var i = 0; i < entries.Length; i++)
            {
                if (ReferenceEquals(entries[i].Set, selectionSet))
                {
                    return entries[i].Children;
                }
            }

            return BuildAndPublish(ref _typeAwareEntries, selectionSet, typeAware: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResultSelectionSet?[] GetTypeUnawareChildren(SelectionSet selectionSet)
        {
            var entries = Volatile.Read(ref _typeUnawareEntries);

            for (var i = 0; i < entries.Length; i++)
            {
                if (ReferenceEquals(entries[i].Set, selectionSet))
                {
                    return entries[i].Children;
                }
            }

            return BuildAndPublish(ref _typeUnawareEntries, selectionSet, typeAware: false);
        }

        private ResultSelectionSet?[] BuildAndPublish(
            ref Entry[] entriesField,
            SelectionSet selectionSet,
            bool typeAware)
        {
            var selections = selectionSet.Selections;
            var children = new ResultSelectionSet?[selections.Length];

            // Built through the existing TryGetChild so direct-first order,
            // fragment order and type filtering are identical by construction.
            // The type-aware map uses the selection set's own type context, which
            // is what the divergence guard requires at the call site.
            for (var i = 0; i < selections.Length; i++)
            {
                children[i] = typeAware
                    ? _resultSelectionSet.TryGetChild(
                        selections[i].ResponseName,
                        selectionSet.Type)
                    : _resultSelectionSet.TryGetChild(selections[i].ResponseName);
            }

            while (true)
            {
                var current = Volatile.Read(ref entriesField);

                // A racing builder may have published first; its content is
                // identical, so return the published entry.
                for (var i = 0; i < current.Length; i++)
                {
                    if (ReferenceEquals(current[i].Set, selectionSet))
                    {
                        return current[i].Children;
                    }
                }

                if (current.Length >= MaxEntries)
                {
                    return children;
                }

                var next = new Entry[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = new Entry(selectionSet, children);

                if (Interlocked.CompareExchange(ref entriesField, next, current) == current)
                {
                    return children;
                }
            }
        }

        private sealed class Entry(SelectionSet set, ResultSelectionSet?[] children)
        {
            public readonly SelectionSet Set = set;
            public readonly ResultSelectionSet?[] Children = children;
        }
    }

    private sealed class NoOpObjectPool<T> : ObjectPool<T> where T : class, new()
    {
        public override T Get() => new();

        public override void Return(T obj)
        {
        }
    }
}
