using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures <see cref="ResultSelectionSet.TryGetChild(string)"/> and its type-filtered
/// overload against a benchmark-local flattened variant.
///
/// The product implementation splits direct-child lookup across an abstract base
/// (ResultSelectionSet) and two subclasses (SmallResultSelectionSet with a linear scan,
/// LargeResultSelectionSet with a dictionary), so every lookup pays a virtual dispatch
/// on TryGetDirectChild and always enters the fragment loop, even for fragment-free sets.
/// The flattened variant is a single sealed class holding both the selections array and
/// the optional dictionary, with a fragments.Length == 0 early exit before the fragment
/// loop. Same lookup semantics for every input.
///
/// The workload is 1,000 lookups per invocation cycling through a fixed probe list that
/// mixes direct hits, hits inside inline fragments, null-child hits, and misses across a
/// small set (4 fields, linear scan), a large set (12 fields, dictionary), and a set with
/// 2 type-conditioned inline fragments.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class ResultSelectionSetLookupBenchmark : FusionBenchmarkBase
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

    // 4 direct fields, no fragments. Produces a SmallResultSelectionSet (linear scan).
    private const string SmallSource =
        """
        {
          id
          name
          price
          dimension { height width }
        }
        """;

    // 12 direct fields, no fragments. Produces a LargeResultSelectionSet (dictionary).
    private const string LargeSource =
        """
        {
          id
          name
          description
          price
          averageRating
          title
          content
          publishedAt
          tags
          dimension { height width }
          author { id displayName }
          reviews { nodes { id body stars } }
        }
        """;

    // 3 direct fields plus 2 inline fragments with type conditions resolved
    // against the composed fusion schema.
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

    // Expected number of non-null results across one pass over the distinct probe
    // list below. Guards against probe typos silently turning the workload into
    // an all-miss loop.
    private const int ExpectedDistinctProbeHits = 9;

    private ResultSelectionSet[] _productSets = null!;
    private FlatResultSelectionSet[] _flatSets = null!;
    private string[] _probeNames = null!;
    private IComplexTypeDefinition?[] _probeTypes = null!;

    [GlobalSetup]
    public void Setup()
    {
        var schema = CreateFusionSchema();

        var smallNode = Utf8GraphQLParser.Syntax.ParseSelectionSet(SmallSource);
        var largeNode = Utf8GraphQLParser.Syntax.ParseSelectionSet(LargeSource);
        var fragmentNode = Utf8GraphQLParser.Syntax.ParseSelectionSet(FragmentSource);

        var smallProduct = ResultSelectionSet.Create(smallNode, schema);
        var largeProduct = ResultSelectionSet.Create(largeNode, schema);
        var fragmentProduct = ResultSelectionSet.Create(fragmentNode, schema);

        // The candidate claim is linear-scan vs dictionary behavior per shape,
        // so the shapes must keep mapping to the expected product classes.
        if (smallProduct is not SmallResultSelectionSet)
        {
            throw new InvalidOperationException(
                "The 4-field shape no longer produces a SmallResultSelectionSet.");
        }

        if (largeProduct is not LargeResultSelectionSet)
        {
            throw new InvalidOperationException(
                "The 12-field shape no longer produces a LargeResultSelectionSet.");
        }

        var smallFlat = FlatResultSelectionSet.Create(smallNode, schema);
        var largeFlat = FlatResultSelectionSet.Create(largeNode, schema);
        var fragmentFlat = FlatResultSelectionSet.Create(fragmentNode, schema);

        if (!schema.Types.TryGetType<IObjectTypeDefinition>("Product", out var productType))
        {
            throw new InvalidOperationException("Product type not found in the composed schema.");
        }

        if (!schema.Types.TryGetType<IObjectTypeDefinition>("Article", out var articleType))
        {
            throw new InvalidOperationException("Article type not found in the composed schema.");
        }

        var productSets = new List<ResultSelectionSet>();
        var flatSets = new List<FlatResultSelectionSet>();
        var names = new List<string>();
        var types = new List<IComplexTypeDefinition?>();

        void Add(
            ResultSelectionSet productSet,
            FlatResultSelectionSet flatSet,
            string name,
            IComplexTypeDefinition? type)
        {
            productSets.Add(productSet);
            flatSets.Add(flatSet);
            names.Add(name);
            types.Add(type);
        }

        // Small set: non-null hit, two null-child hits, miss.
        Add(smallProduct, smallFlat, "dimension", null);
        Add(smallProduct, smallFlat, "id", null);
        Add(smallProduct, smallFlat, "name", null);
        Add(smallProduct, smallFlat, "missing", null);

        // Large set: dictionary hits (non-null and null-child) and a miss.
        Add(largeProduct, largeFlat, "dimension", null);
        Add(largeProduct, largeFlat, "author", null);
        Add(largeProduct, largeFlat, "reviews", null);
        Add(largeProduct, largeFlat, "id", null);
        Add(largeProduct, largeFlat, "tags", null);
        Add(largeProduct, largeFlat, "missing", null);

        // Fragment set, type-unaware overload: direct null-child hits, hits that
        // resolve inside the first and second fragment, a leaf-in-fragment probe
        // (behaves like a miss because fragment hits only count with a non-null
        // child), and a full miss.
        Add(fragmentProduct, fragmentFlat, "id", null);
        Add(fragmentProduct, fragmentFlat, "description", null);
        Add(fragmentProduct, fragmentFlat, "dimension", null);
        Add(fragmentProduct, fragmentFlat, "author", null);
        Add(fragmentProduct, fragmentFlat, "name", null);
        Add(fragmentProduct, fragmentFlat, "missing", null);

        // Fragment set, type-filtered overload: matching and non-matching type
        // conditions, plus a typed miss.
        Add(fragmentProduct, fragmentFlat, "dimension", productType);
        Add(fragmentProduct, fragmentFlat, "dimension", articleType);
        Add(fragmentProduct, fragmentFlat, "author", articleType);
        Add(fragmentProduct, fragmentFlat, "missing", productType);

        // Type-filtered lookups on fragment-free sets: this is where the
        // fragments.Length == 0 early exit applies.
        Add(smallProduct, smallFlat, "dimension", productType);
        Add(largeProduct, largeFlat, "missing", productType);

        VerifyEquivalence(productSets, flatSets, names, types);

        _productSets = new ResultSelectionSet[ProbeCount];
        _flatSets = new FlatResultSelectionSet[ProbeCount];
        _probeNames = new string[ProbeCount];
        _probeTypes = new IComplexTypeDefinition?[ProbeCount];

        for (var i = 0; i < ProbeCount; i++)
        {
            var j = i % names.Count;
            _productSets[i] = productSets[j];
            _flatSets[i] = flatSets[j];
            _probeNames[i] = names[j];
            _probeTypes[i] = types[j];
        }
    }

    private static void VerifyEquivalence(
        List<ResultSelectionSet> productSets,
        List<FlatResultSelectionSet> flatSets,
        List<string> names,
        List<IComplexTypeDefinition?> types)
    {
        var hits = 0;

        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            var type = types[i];

            var expected = type is null
                ? productSets[i].TryGetChild(name)
                : productSets[i].TryGetChild(name, type);

            var actual = type is null
                ? flatSets[i].TryGetChild(name)
                : flatSets[i].TryGetChild(name, type);

            if ((expected is null) != (actual is null))
            {
                throw new InvalidOperationException(
                    $"Probe '{name}' (type: {type?.Name ?? "none"}): product returned "
                    + $"{(expected is null ? "null" : "a child")} but the flattened variant "
                    + $"returned {(actual is null ? "null" : "a child")}.");
            }

            if (expected is not null && actual is not null)
            {
                var expectedSyntax = expected.ToSelectionSetNode().ToString(indented: false);
                var actualSyntax = actual.ToSelectionSetNode().ToString(indented: false);

                if (!string.Equals(expectedSyntax, actualSyntax, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Probe '{name}' (type: {type?.Name ?? "none"}): child selection sets "
                        + $"differ.\nProduct: {expectedSyntax}\nFlattened: {actualSyntax}");
                }

                hits++;
            }
        }

        if (hits != ExpectedDistinctProbeHits)
        {
            throw new InvalidOperationException(
                $"Expected {ExpectedDistinctProbeHits} non-null probe results "
                + $"but observed {hits}. The probe list no longer matches the shapes.");
        }
    }

    /// <summary>
    /// Current product behavior: TryGetChild on the abstract ResultSelectionSet
    /// dispatches virtually to Small/LargeResultSelectionSet.TryGetDirectChild
    /// and always runs the fragment loop on a direct miss.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int TwoClassVirtual_TryGetChild()
    {
        var sets = _productSets;
        var names = _probeNames;
        var types = _probeTypes;
        var hits = 0;

        for (var i = 0; i < names.Length; i++)
        {
            var child = types[i] is { } objectType
                ? sets[i].TryGetChild(names[i], objectType)
                : sets[i].TryGetChild(names[i]);

            if (child is not null)
            {
                hits++;
            }
        }

        return hits;
    }

    /// <summary>
    /// Candidate optimization: single sealed class, non-virtual inlined scan or
    /// dictionary lookup, fragments.Length == 0 early exit before the fragment loop.
    /// </summary>
    [Benchmark]
    public int FlattenedSealed_TryGetChild()
    {
        var sets = _flatSets;
        var names = _probeNames;
        var types = _probeTypes;
        var hits = 0;

        for (var i = 0; i < names.Length; i++)
        {
            var child = types[i] is { } objectType
                ? sets[i].TryGetChild(names[i], objectType)
                : sets[i].TryGetChild(names[i]);

            if (child is not null)
            {
                hits++;
            }
        }

        return hits;
    }

    /// <summary>
    /// Represents a direct field selection in the flattened variant. Mirrors
    /// ResultSelection.cs lines 6-10.
    /// </summary>
    private readonly struct FlatSelection(string responseName, FlatResultSelectionSet? child)
    {
        public string ResponseName { get; } = responseName;
        public FlatResultSelectionSet? Child { get; } = child;
    }

    /// <summary>
    /// Represents an inline fragment in the flattened variant. Mirrors
    /// ResultFragment.cs lines 8-12.
    /// </summary>
    private readonly struct FlatFragment(ITypeDefinition? typeCondition, FlatResultSelectionSet body)
    {
        public ITypeDefinition? TypeCondition { get; } = typeCondition;
        public FlatResultSelectionSet Body { get; } = body;
    }

    /// <summary>
    /// Benchmark-local flattened variant of the candidate optimization. It merges
    /// ResultSelectionSet.cs lines 51-67 and 104-138 (TryGetChild and the fragment
    /// walks), SmallResultSelectionSet.cs lines 16-29 (linear scan), and
    /// LargeResultSelectionSet.cs lines 20-31 (dictionary lookup) into one sealed
    /// class with no virtual TryGetDirectChild and an early fragments.Length == 0
    /// exit. Lookup semantics are byte-identical to the product code, including the
    /// rule that a direct hit with a null child short-circuits the fragment walk and
    /// that fragment hits only count when the resolved child is non-null.
    /// </summary>
    private sealed class FlatResultSelectionSet
    {
        // Mirrors ResultSelectionSet.SmallThreshold (ResultSelectionSet.cs line 18)
        // and the Create split at lines 316-329.
        private const int SmallThreshold = 8;

        private readonly FlatSelection[] _selections;
        private readonly FlatFragment[] _fragments;
        private readonly Dictionary<string, FlatResultSelectionSet?>? _childLookup;

        private FlatResultSelectionSet(FlatSelection[] selections, FlatFragment[] fragments)
        {
            _selections = selections;
            _fragments = fragments;

            if (selections.Length >= SmallThreshold)
            {
                var lookup = new Dictionary<string, FlatResultSelectionSet?>(
                    selections.Length,
                    StringComparer.Ordinal);

                for (var i = 0; i < selections.Length; i++)
                {
                    lookup[selections[i].ResponseName] = selections[i].Child;
                }

                _childLookup = lookup;
            }
        }

        public FlatResultSelectionSet? TryGetChild(string responseName)
        {
            if (_childLookup is null)
            {
                var selections = _selections;

                for (var i = 0; i < selections.Length; i++)
                {
                    if (string.Equals(
                        selections[i].ResponseName,
                        responseName,
                        StringComparison.Ordinal))
                    {
                        return selections[i].Child;
                    }
                }
            }
            else if (_childLookup.TryGetValue(responseName, out var child))
            {
                return child;
            }

            var fragments = _fragments;

            if (fragments.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i].Body.TryGetChild(responseName) is { } result)
                {
                    return result;
                }
            }

            return null;
        }

        public FlatResultSelectionSet? TryGetChild(
            string responseName,
            IComplexTypeDefinition objectType)
        {
            if (_childLookup is null)
            {
                var selections = _selections;

                for (var i = 0; i < selections.Length; i++)
                {
                    if (string.Equals(
                        selections[i].ResponseName,
                        responseName,
                        StringComparison.Ordinal))
                    {
                        return selections[i].Child;
                    }
                }
            }
            else if (_childLookup.TryGetValue(responseName, out var child))
            {
                return child;
            }

            var fragments = _fragments;

            if (fragments.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < fragments.Length; i++)
            {
                ref readonly var fragment = ref fragments[i];

                if (fragment.TypeCondition?.IsAssignableFrom(objectType) == false)
                {
                    continue;
                }

                var result = fragment.Body.TryGetChild(responseName, objectType);

                if (result is not null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds the flattened set from the same AST the product Create consumes.
        /// Mirrors the lookup-relevant parts of ResultSelectionSet.Create
        /// (ResultSelectionSet.cs lines 232-330): response name from alias or name,
        /// recursive child sets, and fragment type conditions resolved through the
        /// schema. Source alias mappings and opaque-element marking are omitted
        /// because they do not participate in TryGetChild.
        /// </summary>
        public static FlatResultSelectionSet Create(
            SelectionSetNode selectionSet,
            ISchemaDefinition? schema)
        {
            var selections = new List<FlatSelection>();
            var fragments = new List<FlatFragment>();

            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                        var name = field.Alias?.Value ?? field.Name.Value;

                        FlatResultSelectionSet? child = null;
                        if (field.SelectionSet is { } childSet)
                        {
                            child = Create(childSet, schema);
                        }

                        selections.Add(new FlatSelection(name, child));
                        break;

                    case InlineFragmentNode inlineFragment:
                        ITypeDefinition? typeCondition = null;
                        if (inlineFragment.TypeCondition is not null)
                        {
                            schema?.Types.TryGetType(
                                inlineFragment.TypeCondition.Name.Value,
                                out typeCondition);
                        }

                        var body = Create(inlineFragment.SelectionSet, schema);
                        fragments.Add(new FlatFragment(typeCondition, body));
                        break;
                }
            }

            return new FlatResultSelectionSet(selections.ToArray(), fragments.ToArray());
        }

        /// <summary>
        /// Reconstructs the selection set syntax for the GlobalSetup equivalence
        /// check. Mirrors ResultSelectionSet.ToSelectionSetNode (ResultSelectionSet.cs
        /// lines 143-196) without the source response name mapping branch, which is
        /// never active here because the sets are built without source aliases.
        /// </summary>
        public SelectionSetNode ToSelectionSetNode()
        {
            var selections = new List<ISelectionNode>();

            foreach (var selection in _selections)
            {
                selections.Add(new FieldNode(
                    selection.ResponseName,
                    selection.Child?.ToSelectionSetNode()));
            }

            foreach (var fragment in _fragments)
            {
                selections.Add(new InlineFragmentNode(
                    null,
                    fragment.TypeCondition is not null
                        ? new NamedTypeNode(fragment.TypeCondition.Name)
                        : null,
                    [],
                    fragment.Body.ToSelectionSetNode()));
            }

            return new SelectionSetNode(selections);
        }
    }
}
