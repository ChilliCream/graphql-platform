using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures child selection-set resolution for abstract-typed selections on the value
/// completion hot path. <c>Selection.GetSelectionSet(IComplexTypeDefinition)</c>
/// (src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Nodes/Selection.cs lines 128-153)
/// caches the child selection set in <c>_childSelectionSet</c> only when the field's named
/// type is a concrete object type (lines 138-150). Interface and union typed fields fall
/// through at line 152 to <c>Operation.GetSelectionSet(selection, typeContext)</c>
/// (Execution/Nodes/Operation.cs lines 160-189), which builds a
/// <c>(selection.Id, typeContext.Name)</c> tuple key (line 166) and probes a
/// <c>ConcurrentDictionary</c> (line 168), hashing the type-name string on every probe.
/// The hot callers pay this once per newly materialized abstract object element
/// (ValueCompletion.cs line 1118) and per merge target initialization (lines 239 and 272),
/// so a 1000-element interface list pays 1000 string-hash dictionary probes to fetch the
/// same handful of plan-stable <c>SelectionSet</c> instances.
///
/// The candidate adds a plan-lifetime copy-on-write array of (type, selection set) entries
/// on <c>Selection</c>, scanned by reference identity and capped at 8 entries. Misses fall
/// back to the operation dictionary unchanged, so behavior is identical by construction:
/// <c>Operation.GetSelectionSet</c> only ever publishes one sealed instance per key.
/// The baseline calls the real product method; the optimized variant is a benchmark-local
/// replica of <c>Selection.GetSelectionSet</c> with the candidate scan inserted at the
/// abstract fall-through. <c>DistinctTypes</c> cycles 1 (homogeneous list, the common
/// case), 2 (alternating types), 3, and 10 (two types beyond the 8-entry cap, the
/// worst-case regression shape) concrete implementers across 1000 probes per invocation.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class AbstractSelectionSetCacheBenchmark
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

    private const int ProbeCount = 1000;
    private const int MaxCachedTypeContexts = 8;
    private const string OperationId = "123456789101112";

    private static readonly string[] s_implementerNames =
    [
        "Product",
        "Article",
        "Brand",
        "Category",
        "Promotion",
        "Vendor",
        "Review",
        "Store",
        "Banner",
        "Coupon"
    ];

    private static readonly int[] s_allDistinctTypeShapes = [1, 2, 3, 10];

    private Selection _selection = null!;
    private CachedSelectionResolver _cachedResolver = null!;
    private IComplexTypeDefinition[] _allTypeContexts = null!;
    private IComplexTypeDefinition[] _probeTypeContexts = null!;

    public long Consumed;

    /// <summary>
    /// The number of distinct concrete types cycling through the 1000 probes:
    /// 1 is a homogeneous list, 2 the alternating shape that thrashes single-slot
    /// designs, 3 a small heterogeneous list, and 10 exceeds the 8-entry cap so two
    /// of the ten types always miss the candidate cache and fall back to the
    /// operation dictionary after a full 8-entry scan.
    /// </summary>
    [Params(1, 2, 3, 10)]
    public int DistinctTypes;

    [GlobalSetup]
    public void Setup()
    {
        var schema = ComposeSchema();

        var document = Utf8GraphQLParser.Parse(
            """
            {
              searchContent(query: "bench") {
                id
                title
              }
            }
            """);
        var operationDefinition = (OperationDefinitionNode)document.Definitions[0];

        var compiler = new OperationCompiler(
            schema,
            new NoOpObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operation = compiler.Compile(OperationId, OperationId, OperationId, operationDefinition);

        if (!operation.RootSelectionSet.TryGetSelection("searchContent", out var selection))
        {
            throw new InvalidOperationException(
                "The compiled operation has no searchContent selection.");
        }

        if (selection.NamedType is not FusionInterfaceTypeDefinition)
        {
            throw new InvalidOperationException(
                "searchContent must return an interface so the abstract fall-through "
                + "(Selection.cs line 152) is what both variants measure.");
        }

        _selection = selection;
        _cachedResolver = new CachedSelectionResolver(selection);

        _allTypeContexts = new IComplexTypeDefinition[s_implementerNames.Length];

        for (var i = 0; i < s_implementerNames.Length; i++)
        {
            _allTypeContexts[i] =
                schema.Types.GetType<FusionObjectTypeDefinition>(s_implementerNames[i]);
        }

        // Warm both paths once per type context in a fixed order. This forces the lazy
        // CompileSelectionSet under the operation lock (Operation.cs lines 170-185)
        // exactly once per type, so the measured loops pay pure probe cost. It also
        // fills the candidate cache deterministically: the first 8 types occupy the
        // capped entries and the last 2 always miss, which is the beyond-cap shape
        // of the 10-type parameter.
        foreach (var typeContext in _allTypeContexts)
        {
            var expected = _selection.GetSelectionSet(typeContext);
            var actual = _cachedResolver.GetSelectionSet(typeContext);

            if (expected is null || !ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"Warmup mismatch for type '{typeContext.Name}': the cached variant "
                    + "did not return the product selection set instance.");
            }
        }

        if (_cachedResolver.CachedEntryCount != MaxCachedTypeContexts)
        {
            throw new InvalidOperationException(
                $"The candidate cache holds {_cachedResolver.CachedEntryCount} entries "
                + $"after warmup but the cap is {MaxCachedTypeContexts}.");
        }

        // Verify equivalence over every parameter shape, not just the active one.
        foreach (var distinctTypes in s_allDistinctTypeShapes)
        {
            VerifyEquivalence(BuildProbeSequence(distinctTypes));
        }

        _probeTypeContexts = BuildProbeSequence(DistinctTypes);
    }

    /// <summary>
    /// Current product behavior: every probe calls the real
    /// <c>Selection.GetSelectionSet(IComplexTypeDefinition)</c>, whose abstract
    /// fall-through (Selection.cs line 152) probes the operation's
    /// string-keyed ConcurrentDictionary per call (Operation.cs lines 166-168).
    /// </summary>
    [Benchmark(Baseline = true)]
    public long Baseline_OperationDictionaryProbe()
    {
        var sum = RunBaseline(_probeTypeContexts);
        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Candidate: the same resolution fronted by a plan-lifetime copy-on-write array
    /// of (type, selection set) entries scanned by reference identity; only misses
    /// beyond the 8-entry cap still reach the operation dictionary.
    /// </summary>
    [Benchmark]
    public long Cached_CopyOnWriteArrayScan()
    {
        var sum = RunCached(_probeTypeContexts);
        Consumed = sum;
        return sum;
    }

    private long RunBaseline(IComplexTypeDefinition[] probes)
    {
        var selection = _selection;
        var sum = 0L;

        for (var i = 0; i < probes.Length; i++)
        {
            sum += selection.GetSelectionSet(probes[i])!.Id;
        }

        return sum;
    }

    private long RunCached(IComplexTypeDefinition[] probes)
    {
        var resolver = _cachedResolver;
        var sum = 0L;

        for (var i = 0; i < probes.Length; i++)
        {
            sum += resolver.GetSelectionSet(probes[i])!.Id;
        }

        return sum;
    }

    private IComplexTypeDefinition[] BuildProbeSequence(int distinctTypes)
    {
        var probes = new IComplexTypeDefinition[ProbeCount];

        for (var i = 0; i < probes.Length; i++)
        {
            probes[i] = _allTypeContexts[i % distinctTypes];
        }

        return probes;
    }

    private void VerifyEquivalence(IComplexTypeDefinition[] probes)
    {
        for (var i = 0; i < probes.Length; i++)
        {
            var expected = _selection.GetSelectionSet(probes[i]);
            var actual = _cachedResolver.GetSelectionSet(probes[i]);

            if (expected is null || !ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"Probe {i} ('{probes[i].Name}'): baseline and cached variants "
                    + "resolved different selection set instances.");
            }
        }

        var baselineSum = RunBaseline(probes);
        var cachedSum = RunCached(probes);

        if (baselineSum != cachedSum)
        {
            throw new InvalidOperationException(
                $"Checksum mismatch: baseline {baselineSum}, cached {cachedSum}.");
        }
    }

    /// <summary>
    /// Benchmark-local stand-in for a <c>Selection</c> carrying the candidate cache field.
    /// The product change would add the copy-on-write array as a field on Selection itself;
    /// this wrapper reproduces <c>Selection.GetSelectionSet(IComplexTypeDefinition)</c>
    /// (Selection.cs lines 128-153) statement for statement, with the candidate scan
    /// inserted at the abstract fall-through (line 152).
    /// </summary>
    private sealed class CachedSelectionResolver
    {
        private readonly Selection _selection;

        // Stand-in for the private Selection._childSelectionSet field. It stays null for
        // abstract named types exactly as in the product, so the measured abstract path
        // executes the same null check and type test as the product method.
        private SelectionSet? _childSelectionSet;

        // The candidate field: a copy-on-write array of (type, set) entries for abstract
        // named types, initially null, published via Interlocked.CompareExchange. A lost
        // race drops an entry but never changes results because the operation dictionary
        // returns the identical instance on the retry.
        private SelectionSetCacheEntry[]? _abstractSelectionSets;

        public CachedSelectionResolver(Selection selection)
        {
            _selection = selection;
        }

        public int CachedEntryCount
            => Volatile.Read(ref _abstractSelectionSets)?.Length ?? 0;

        public SelectionSet? GetSelectionSet(IComplexTypeDefinition typeContext)
        {
            var selection = _selection;

            // Mirrors the leaf gate at Selection.cs lines 130-133. IsLeaf reads the same
            // flag bit the product method tests.
            if (selection.IsLeaf)
            {
                return null;
            }

            // Mirrors the concrete-type fast path at Selection.cs lines 138-150.
            var childSelectionSet = _childSelectionSet;

            if (childSelectionSet is not null)
            {
                return childSelectionSet;
            }

            if (selection.NamedType is IObjectTypeDefinition)
            {
                childSelectionSet =
                    selection.DeclaringSelectionSet.DeclaringOperation
                        .GetSelectionSet(selection, typeContext);
                _childSelectionSet = childSelectionSet;
                return childSelectionSet;
            }

            // Candidate change: scan the copy-on-write array by reference identity
            // before falling through to Operation.GetSelectionSet (Selection.cs
            // line 152). Type definition instances are schema singletons, so
            // reference equality is sound; a miss degrades to today's behavior.
            var entries = Volatile.Read(ref _abstractSelectionSets);

            if (entries is not null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];

                    if (ReferenceEquals(entry.Type, typeContext))
                    {
                        return entry.Set;
                    }
                }
            }

            var selectionSet =
                selection.DeclaringSelectionSet.DeclaringOperation
                    .GetSelectionSet(selection, typeContext);

            if (entries is null)
            {
                var initial = new SelectionSetCacheEntry[1];
                initial[0] = new SelectionSetCacheEntry(typeContext, selectionSet);
                Interlocked.CompareExchange(ref _abstractSelectionSets, initial, null);
            }
            else if (entries.Length < MaxCachedTypeContexts)
            {
                var updated = new SelectionSetCacheEntry[entries.Length + 1];
                Array.Copy(entries, updated, entries.Length);
                updated[entries.Length] = new SelectionSetCacheEntry(typeContext, selectionSet);
                Interlocked.CompareExchange(ref _abstractSelectionSets, updated, entries);
            }

            return selectionSet;
        }
    }

    private readonly struct SelectionSetCacheEntry
    {
        public SelectionSetCacheEntry(ITypeDefinition type, SelectionSet set)
        {
            Type = type;
            Set = set;
        }

        public readonly ITypeDefinition Type;

        public readonly SelectionSet Set;
    }

    /// <summary>
    /// Composes a fusion schema whose SearchResult interface has ten implementers so the
    /// benchmark can cycle more runtime types than the candidate's 8-entry cap.
    /// Composition recipe follows FusionBenchmarkBase.CreateFusionSchema.
    /// </summary>
    private static FusionSchemaDefinition ComposeSchema()
    {
        var sdl = new StringBuilder();
        sdl.Append(
            """
            type Query {
              searchContent(query: String!): [SearchResult!]!
            }

            interface SearchResult {
              id: ID!
              title: String!
            }
            """);

        foreach (var name in s_implementerNames)
        {
            sdl.Append("\n\n");
            sdl.Append("type ").Append(name).Append(" implements SearchResult {\n");
            sdl.Append("  id: ID!\n");
            sdl.Append("  title: String!\n");
            sdl.Append('}');
        }

        List<SourceSchemaText> sourceSchemas = [new SourceSchemaText("search", sdl.ToString())];

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private sealed class NoOpObjectPool<T> : ObjectPool<T> where T : class, new()
    {
        public override T Get()
        {
            return new T();
        }

        public override void Return(T obj)
        {
        }
    }
}
