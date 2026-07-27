using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures abstract-type resolution (__typename to object type) for list elements when the
/// abstract type has more than <c>FusionInterfaceTypeDefinition.MaxTypeNameLookupTypes</c> (4)
/// implementers. On that path <c>ValueCompletion.TryResolveType</c> declines and the current
/// code pays, per element, an <c>AssertString()</c> (UTF-16 transcode plus string allocation)
/// and a string-keyed dictionary lookup via <c>schema.Types.GetType&lt;IObjectTypeDefinition&gt;</c>.
/// The candidate keeps a single-entry memo of the last resolved
/// <see cref="FusionObjectTypeDefinition"/> and hits it with
/// <c>TryGetRawStringValue</c> + <c>Ascii.Equals</c>, which is allocation free.
/// Lists are typically homogeneous, so the memo hits after the first element; the mixed
/// payload alternates two type names every element, which is the worst case for the memo
/// (100 percent misses) and shows the miss-path parity.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class AbstractTypeResolutionMemoBenchmark
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

    private const int ElementCount = 1000;

    // Field types mirror ValueCompletion: _schema is ISchemaDefinition
    // (ValueCompletion.cs line 21), so Types/GetType go through the same
    // interface dispatch the product pays.
    private ISchemaDefinition _schema = null!;
    private ITypeDefinition _abstractType = null!;
    private MemoryArena _arena = null!;
    private SourceResultDocument _homogeneousDoc = null!;
    private SourceResultDocument _mixedDoc = null!;

    // The candidate memo: a single-entry cache of the last type resolved through
    // the global-name fallback. In the product this would be an instance field of
    // ValueCompletion, which only runs under FetchResultStore's lock.
    private FusionObjectTypeDefinition? _lastResolvedObjectType;

    // The two-entry MRU memo: slot A is the most recently used type, slot B the
    // one before it. A hit on slot B swaps the slots so the most recent stays
    // first; a miss installs the newly resolved type as slot A and demotes the
    // old A to B. This keeps the alternating two-type payload on the hit path.
    private FusionObjectTypeDefinition? _memoTypeA;
    private FusionObjectTypeDefinition? _memoTypeB;

    [GlobalSetup]
    public void Setup()
    {
        var fusionSchema = ComposeSchema();
        _schema = fusionSchema;

        var interfaceType = fusionSchema.Types.GetType<FusionInterfaceTypeDefinition>("SearchResult");

        // Mirrors OperationCompiler.cs lines 333-338: the compiler prepares the
        // __typename lookup table once per interface. With six implementers the
        // prepared table stays empty, so TryResolveType declines for every element
        // and both variants exercise the global-name fallback under test.
        // TypeNameLookupTypes and PrepareTypeNameLookupTypes are internal to the
        // HotChocolate.Fusion.Execution.Types assembly, which grants no
        // InternalsVisibleTo to this benchmark assembly, so this one-time setup
        // step is replayed through reflection. Reflection runs only inside
        // GlobalSetup; the measured code calls the real TryResolveType.
        var typeNameLookupTypesProperty = typeof(FusionInterfaceTypeDefinition).GetProperty(
            "TypeNameLookupTypes",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "FusionInterfaceTypeDefinition.TypeNameLookupTypes was not found.");
        var prepareTypeNameLookupTypesMethod = typeof(FusionInterfaceTypeDefinition).GetMethod(
            "PrepareTypeNameLookupTypes",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "FusionInterfaceTypeDefinition.PrepareTypeNameLookupTypes was not found.");

        var lookupTypes = (ImmutableArray<FusionObjectTypeDefinition>)
            typeNameLookupTypesProperty.GetValue(interfaceType)!;

        if (lookupTypes.IsDefault)
        {
            prepareTypeNameLookupTypesMethod.Invoke(
                interfaceType,
                [fusionSchema.GetPossibleTypes(interfaceType)]);

            lookupTypes = (ImmutableArray<FusionObjectTypeDefinition>)
                typeNameLookupTypesProperty.GetValue(interfaceType)!;
        }

        if (!lookupTypes.IsEmpty)
        {
            throw new InvalidOperationException(
                "The interface must have more than 4 implementers so that "
                + "TryResolveType declines and the fallback path is measured.");
        }

        _abstractType = interfaceType;

        _arena = new MemoryArena();
        _homogeneousDoc = ParsePayload(BuildPayload(mixed: false));
        _mixedDoc = ParsePayload(BuildPayload(mixed: true));

        VerifyEquivalence(_homogeneousDoc);
        VerifyEquivalence(_mixedDoc);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _arena.Dispose();
    }

    [Benchmark(Baseline = true)]
    public int Baseline_Homogeneous()
    {
        return RunBaseline(_homogeneousDoc);
    }

    [Benchmark]
    public int Memoized_Homogeneous()
    {
        return RunMemoized(_homogeneousDoc);
    }

    [Benchmark]
    public int Baseline_MixedTwoTypes()
    {
        return RunBaseline(_mixedDoc);
    }

    [Benchmark]
    public int Memoized_MixedTwoTypes()
    {
        return RunMemoized(_mixedDoc);
    }

    [Benchmark]
    public int Memoized2_Homogeneous()
    {
        return RunMemoized2(_homogeneousDoc);
    }

    [Benchmark]
    public int Memoized2_MixedTwoTypes()
    {
        return RunMemoized2(_mixedDoc);
    }

    private int RunBaseline(SourceResultDocument doc)
    {
        var total = 0;

        foreach (var element in doc.Root.EnumerateArray())
        {
            total += ResolveTypeBaseline(element).Name.Length;
        }

        return total;
    }

    private int RunMemoized(SourceResultDocument doc)
    {
        // A fresh ValueCompletion instance is created per request, so each
        // invocation starts with an empty memo: the first element misses and
        // every later element of a homogeneous list hits.
        _lastResolvedObjectType = null;

        var total = 0;

        foreach (var element in doc.Root.EnumerateArray())
        {
            total += ResolveTypeMemoized(element).Name.Length;
        }

        return total;
    }

    private int RunMemoized2(SourceResultDocument doc)
    {
        // Same per-request reset as RunMemoized: both slots start empty.
        _memoTypeA = null;
        _memoTypeB = null;

        var total = 0;

        foreach (var element in doc.Root.EnumerateArray())
        {
            total += ResolveTypeMemoized2(element).Name.Length;
        }

        return total;
    }

    /// <summary>
    /// Byte-faithful benchmark-local copy of the abstract branch of the private
    /// ValueCompletion.GetType, mirroring
    /// src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Results/ValueCompletion.cs
    /// lines 1297-1309 (the object-type and isOpaque early exits at lines 1284-1295
    /// never apply here because the selection type is the abstract interface).
    /// TryResolveType is the real product method (internal static, same file
    /// lines 1313-1342), reachable through InternalsVisibleTo.
    /// </summary>
    private IObjectTypeDefinition ResolveTypeBaseline(SourceResultElement data)
    {
        var typeNameElement = data.GetProperty(IntrospectionFieldNames.TypeNameSpan);

        if (ValueCompletion.TryResolveType(typeNameElement, _abstractType, out var resolvedType))
        {
            return resolvedType;
        }

        var typeName = typeNameElement.AssertString();
        return _schema.Types.GetType<IObjectTypeDefinition>(typeName);
    }

    /// <summary>
    /// Candidate variant: identical to <see cref="ResolveTypeBaseline"/> except a
    /// single-entry memo fronts the global-name fallback. A memo hit compares the
    /// raw UTF-8 __typename bytes against the memoized type's name, skipping the
    /// string allocation and the dictionary lookup. When TryGetRawStringValue
    /// declines (escaped or chunk-spanning value) the existing fallback runs
    /// unchanged, preserving semantics exactly.
    /// </summary>
    private IObjectTypeDefinition ResolveTypeMemoized(SourceResultElement data)
    {
        var typeNameElement = data.GetProperty(IntrospectionFieldNames.TypeNameSpan);

        if (ValueCompletion.TryResolveType(typeNameElement, _abstractType, out var resolvedType))
        {
            return resolvedType;
        }

        if (_lastResolvedObjectType is { } last
            && typeNameElement.TryGetRawStringValue(out var rawTypeName)
            && Ascii.Equals(rawTypeName, last.Name))
        {
            return last;
        }

        var typeName = typeNameElement.AssertString();
        var objectType = _schema.Types.GetType<IObjectTypeDefinition>(typeName);

        if (objectType is FusionObjectTypeDefinition fusionObjectType)
        {
            _lastResolvedObjectType = fusionObjectType;
        }

        return objectType;
    }

    /// <summary>
    /// Two-entry MRU variant: the raw UTF-8 __typename is read once per element
    /// and compared against both memo slots, most recent first. A slot B hit
    /// swaps the slots so the just-seen type is checked first on the next
    /// element, which keeps an alternating two-type list entirely on the hit
    /// path. A miss runs the unchanged fallback and installs the result as
    /// slot A, demoting the old A to B. When TryGetRawStringValue declines
    /// (escaped or chunk-spanning value) the fallback runs unchanged,
    /// preserving semantics exactly.
    /// </summary>
    private IObjectTypeDefinition ResolveTypeMemoized2(SourceResultElement data)
    {
        var typeNameElement = data.GetProperty(IntrospectionFieldNames.TypeNameSpan);

        if (ValueCompletion.TryResolveType(typeNameElement, _abstractType, out var resolvedType))
        {
            return resolvedType;
        }

        if (typeNameElement.TryGetRawStringValue(out var rawTypeName))
        {
            if (_memoTypeA is { } typeA && Ascii.Equals(rawTypeName, typeA.Name))
            {
                return typeA;
            }

            if (_memoTypeB is { } typeB && Ascii.Equals(rawTypeName, typeB.Name))
            {
                _memoTypeB = _memoTypeA;
                _memoTypeA = typeB;
                return typeB;
            }
        }

        var typeName = typeNameElement.AssertString();
        var objectType = _schema.Types.GetType<IObjectTypeDefinition>(typeName);

        if (objectType is FusionObjectTypeDefinition fusionObjectType)
        {
            _memoTypeB = _memoTypeA;
            _memoTypeA = fusionObjectType;
        }

        return objectType;
    }

    private void VerifyEquivalence(SourceResultDocument doc)
    {
        var baselineTypes = new List<IObjectTypeDefinition>(ElementCount);

        foreach (var element in doc.Root.EnumerateArray())
        {
            var type = ResolveTypeBaseline(element);

            if (!element.GetProperty(IntrospectionFieldNames.TypeNameSpan).ValueEquals(type.Name))
            {
                throw new InvalidOperationException(
                    $"Baseline resolved '{type.Name}' for an element whose "
                    + "__typename does not match.");
            }

            baselineTypes.Add(type);
        }

        _lastResolvedObjectType = null;
        var memoizedTypes = new List<IObjectTypeDefinition>(ElementCount);

        foreach (var element in doc.Root.EnumerateArray())
        {
            memoizedTypes.Add(ResolveTypeMemoized(element));
        }

        _memoTypeA = null;
        _memoTypeB = null;
        var memoized2Types = new List<IObjectTypeDefinition>(ElementCount);

        foreach (var element in doc.Root.EnumerateArray())
        {
            memoized2Types.Add(ResolveTypeMemoized2(element));
        }

        if (baselineTypes.Count != ElementCount
            || memoizedTypes.Count != ElementCount
            || memoized2Types.Count != ElementCount)
        {
            throw new InvalidOperationException(
                $"Expected {ElementCount} resolved types but baseline produced "
                + $"{baselineTypes.Count}, memoized produced {memoizedTypes.Count} "
                + $"and memoized2 produced {memoized2Types.Count}.");
        }

        for (var i = 0; i < ElementCount; i++)
        {
            if (!ReferenceEquals(baselineTypes[i], memoizedTypes[i]))
            {
                throw new InvalidOperationException(
                    $"Type mismatch at element {i}: baseline resolved "
                    + $"'{baselineTypes[i].Name}' but memoized resolved "
                    + $"'{memoizedTypes[i].Name}' (or a different instance).");
            }

            if (!ReferenceEquals(baselineTypes[i], memoized2Types[i]))
            {
                throw new InvalidOperationException(
                    $"Type mismatch at element {i}: baseline resolved "
                    + $"'{baselineTypes[i].Name}' but memoized2 resolved "
                    + $"'{memoized2Types[i].Name}' (or a different instance).");
            }
        }
    }

    private SourceResultDocument ParsePayload(byte[] payload)
    {
        return SourceResultDocument.Parse(_arena, payload, payload.Length);
    }

    private static byte[] BuildPayload(bool mixed)
    {
        var json = new StringBuilder(ElementCount * 64);
        json.Append('[');

        for (var i = 0; i < ElementCount; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            var typeName = mixed && i % 2 == 1 ? "Article" : "Product";
            json.Append("{\"__typename\":\"").Append(typeName).Append("\",");
            json.Append("\"id\":\"id-").Append(i).Append("\",");
            json.Append("\"title\":\"Item ").Append(i).Append("\"}");
        }

        json.Append(']');
        return Encoding.UTF8.GetBytes(json.ToString());
    }

    /// <summary>
    /// Composes a minimal fusion schema whose SearchResult interface has six
    /// implementers, which exceeds FusionInterfaceTypeDefinition.MaxTypeNameLookupTypes (4)
    /// so TypeNameLookupTypes stays empty and every element takes the global-name
    /// fallback that the candidate targets. Composition recipe follows
    /// FusionBenchmarkBase.CreateFusionSchema.
    /// </summary>
    private static FusionSchemaDefinition ComposeSchema()
    {
        List<SourceSchemaText> sourceSchemas =
        [
            new SourceSchemaText(
                "search",
                """
                type Query {
                  searchContent(query: String!): [SearchResult!]!
                }

                interface SearchResult {
                  id: ID!
                  title: String!
                }

                type Product implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Article implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Brand implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Category implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Promotion implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Vendor implements SearchResult {
                  id: ID!
                  title: String!
                }
                """)
        ];

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
}
