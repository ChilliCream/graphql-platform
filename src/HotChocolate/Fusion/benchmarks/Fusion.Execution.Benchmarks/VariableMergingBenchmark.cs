using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;
using FusionNameNode = HotChocolate.Fusion.Language.NameNode;
using IntValueNode = HotChocolate.Language.IntValueNode;
using StringValueNode = HotChocolate.Language.StringValueNode;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;
using IValueNode = HotChocolate.Language.IValueNode;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures variable materialization from deferred snapshots across representative input shapes.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(
    RuntimeMoniker.Net10_0,
    launchCount: 3,
    warmupCount: 15,
    iterationCount: 20,
    invocationCount: 1)]
public class VariableMergingBenchmark : FusionBenchmarkBase
{
    private const string OperationId = "123456789101112";
    private const int MaxRetainedLength = 256;

    // Each iteration performs one bounded invocation against reusable stores.
    private const int N1OperationsPerInvoke = 32_768;
    private const int N10OperationsPerInvoke = 4_096;
    private const int N100OperationsPerInvoke = 512;
    private const int N1000OperationsPerInvoke = 64;

    private static readonly IReadOnlyList<ObjectFieldNode> s_oneForwardedVariable =
        [new ObjectFieldNode("limit", new IntValueNode(10))];

    private static readonly IReadOnlyList<ObjectFieldNode> s_noForwardedVariables = [];

    private static readonly OperationRequirement[] s_singleRequirement =
        [Requirement("__fusion_1_id")];

    private static readonly OperationRequirement[] s_missingRequirement =
        [Requirement("__fusion_2_sku")];

    private static readonly HashSet<string> s_oneImportedKey =
        new(["__fusion_1_id"], StringComparer.Ordinal);

    private static readonly HashSet<string> s_twoImportedKeys =
        new(["__fusion_1_id", "__fusion_2_sku"], StringComparer.Ordinal);

    private FetchResultStore _sourceStore = null!;
    private FetchResultStore _baselineStore = null!;
    private FetchResultStore _snapshotStore = null!;
    private MemoryArena _baselineArena = null!;
    private MemoryArena _snapshotArena = null!;
    private FusionSchemaDefinition _schema = null!;
    private HotChocolate.Fusion.Execution.Nodes.Operation _operation = null!;

    private ImmutableArray<VariableValues> _singleEntrySnapshot;
    private ImmutableArray<VariableValues> _subsetEntrySnapshot;
    private ImmutableArray<VariableValues> _list1Snapshot;
    private ImmutableArray<VariableValues> _list10Snapshot;
    private ImmutableArray<VariableValues> _list100Snapshot;
    private ImmutableArray<VariableValues> _list1000Snapshot;
    private ImmutableArray<VariableValues> _list1000DuplicateHeavySnapshot;
    private ImmutableArray<VariableValues> _deepLargeSnapshot;

    [GlobalSetup]
    public void Setup()
    {
        _schema = CreateFusionSchema();
        var documentRewriter = new DocumentRewriter(_schema);
        var operationDefinition = documentRewriter
            .RewriteDocument(
                Utf8GraphQLParser.Parse(
                    "{ products { nodes { id } } }"))
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var fieldMapPool = new DefaultObjectPool<
            OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<
                    OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operationCompiler = new OperationCompiler(_schema, fieldMapPool);
        _operation = operationCompiler.Compile(
            OperationId,
            OperationId,
            OperationId,
            operationDefinition);

        // The source store supplies imported parent rows to the deferred snapshot.
        _sourceStore = new FetchResultStore();
        _baselineStore = new FetchResultStore();
        _snapshotStore = new FetchResultStore();

        _singleEntrySnapshot =
        [
            CreateImportedEntry(
                _sourceStore,
                CompactPath.Root,
                Field("__fusion_1_id", new StringValueNode("1")))
        ];

        _subsetEntrySnapshot =
        [
            CreateImportedEntry(
                _sourceStore,
                CompactPath.Root,
                Field("__fusion_1_id", new StringValueNode("1")),
                Field("__fusion_2_sku", new StringValueNode("sku-1")))
        ];

        _list1Snapshot = BuildListSnapshot(_sourceStore, count: 1, uniqueValueCount: 1);
        _list10Snapshot = BuildListSnapshot(_sourceStore, count: 10, uniqueValueCount: 10);
        _list100Snapshot = BuildListSnapshot(_sourceStore, count: 100, uniqueValueCount: 100);
        _list1000Snapshot = BuildListSnapshot(
            _sourceStore,
            count: 1000,
            uniqueValueCount: 1000);
        _list1000DuplicateHeavySnapshot = BuildListSnapshot(
            _sourceStore,
            count: 1000,
            uniqueValueCount: 10);
        _deepLargeSnapshot =
        [
            CreateImportedEntry(
                _sourceStore,
                CompactPath.Root,
                Field("__fusion_1_id", CreateDeepLargeValue(depth: 8, width: 128)))
        ];
    }

    [IterationSetup]
    public void SetupIteration()
    {
        _baselineArena = new MemoryArena();
        _snapshotArena = new MemoryArena();
        InitializeStore(_baselineStore, _baselineArena);
        InitializeStore(_snapshotStore, _snapshotArena);
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        // Clean the reusable stores between measured iterations.
        _baselineStore.Clean(MaxRetainedLength, MaxRetainedLength);
        _snapshotStore.Clean(MaxRetainedLength, MaxRetainedLength);
        _baselineArena.Dispose();
        _snapshotArena.Dispose();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _sourceStore.Dispose();
        _baselineStore.Dispose();
        _snapshotStore.Dispose();
    }

    /// <summary>
    /// Existing non-defer per-entry write path through the same
    /// <see cref="FetchResultStore"/> writer used by the general overloads.
    /// The full non-defer overload also walks the result document via
    /// CollectTargetElements, which depends on a populated CompositeResultDocument
    /// and is intentionally outside the scope of this benchmark. This method is
    /// not a semantic baseline for the snapshot methods.
    /// </summary>
    [Benchmark(OperationsPerInvoke = N1OperationsPerInvoke)]
    public VariableValues NonDefer_PerEntryWrite()
    {
        var result = default(VariableValues);

        for (var i = 0; i < N1OperationsPerInvoke; i++)
        {
            result = _baselineStore.CreateVariableValueSets(
                CompactPath.Root,
                s_oneForwardedVariable);
        }

        return result;
    }

    /// <summary>
    /// Case E with one forwarded variable and one requested requirement.
    /// </summary>
    [Benchmark(OperationsPerInvoke = N1OperationsPerInvoke)]
    public ImmutableArray<VariableValues> Defer_Snapshot_OneForwardedOneRequirement()
        => RunSnapshotMerge(
            _singleEntrySnapshot,
            s_oneImportedKey,
            s_oneForwardedVariable,
            N1OperationsPerInvoke);

    /// <summary>
    /// Case E strict subset filter. The imported snapshot carries two keys but
    /// only one is requested, so the merge writes a smaller variable object.
    /// </summary>
    [Benchmark(OperationsPerInvoke = N1OperationsPerInvoke)]
    public ImmutableArray<VariableValues> Defer_Snapshot_StrictSubset()
        => RunSnapshotMerge(
            _subsetEntrySnapshot,
            s_twoImportedKeys,
            s_noForwardedVariables,
            N1OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N1OperationsPerInvoke)]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N1()
        => RunSnapshotMerge(
            _list1Snapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            N1OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N10OperationsPerInvoke)]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N10()
        => RunSnapshotMerge(
            _list10Snapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            N10OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N100OperationsPerInvoke)]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N100()
        => RunSnapshotMerge(
            _list100Snapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            N100OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N1000OperationsPerInvoke)]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N1000()
        => RunSnapshotMerge(
            _list1000Snapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            N1000OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N1000OperationsPerInvoke)]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N1000_DuplicateHeavy()
        => RunSnapshotMerge(
            _list1000DuplicateHeavySnapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            N1000OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N10OperationsPerInvoke)]
    public bool Defer_Eligibility_Missing_List_N10()
        => RunEligibilityProbe(_list10Snapshot, N10OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N100OperationsPerInvoke)]
    public bool Defer_Eligibility_Missing_List_N100()
        => RunEligibilityProbe(_list100Snapshot, N100OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N1000OperationsPerInvoke)]
    public bool Defer_Eligibility_Missing_List_N1000()
        => RunEligibilityProbe(_list1000Snapshot, N1000OperationsPerInvoke);

    [Benchmark(OperationsPerInvoke = N100OperationsPerInvoke)]
    public bool Defer_Eligibility_Missing_DeepLargeValue()
        => RunEligibilityProbe(_deepLargeSnapshot, N100OperationsPerInvoke);

    private ImmutableArray<VariableValues> RunSnapshotMerge(
        ImmutableArray<VariableValues> importedEntries,
        HashSet<string> importedKeys,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        int operations)
    {
        var result = default(ImmutableArray<VariableValues>);

        for (var i = 0; i < operations; i++)
        {
            result = _snapshotStore.CreateVariableValueSetsFromSnapshot(
                importedEntries,
                importedKeys,
                requestVariables,
                s_singleRequirement);
        }

        return result;
    }

    private bool RunEligibilityProbe(
        ImmutableArray<VariableValues> importedEntries,
        int operations)
    {
        var result = false;

        for (var i = 0; i < operations; i++)
        {
            result = _snapshotStore.HasVariableValueSetFromSnapshot(
                importedEntries,
                s_twoImportedKeys,
                s_missingRequirement);
        }

        return result;
    }

    private static ImmutableArray<VariableValues> BuildListSnapshot(
        FetchResultStore source,
        int count,
        int uniqueValueCount)
    {
        var builder = ImmutableArray.CreateBuilder<VariableValues>(count);

        for (var i = 0; i < count; i++)
        {
            builder.Add(
                CreateImportedEntry(
                    source,
                    Path(i),
                    Field(
                        "__fusion_1_id",
                        new StringValueNode((i % uniqueValueCount).ToString()))));
        }

        return builder.MoveToImmutable();
    }

    private static VariableValues CreateImportedEntry(
        FetchResultStore store,
        CompactPath path,
        params ObjectFieldNode[] fields)
        => store.CreateVariableValueSets(path, fields);

    private static ObjectFieldNode Field(string name, IValueNode value)
        => new(name, value);

    private static IValueNode CreateDeepLargeValue(int depth, int width)
    {
        var items = new IValueNode[width];

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new StringValueNode(i.ToString());
        }

        IValueNode value = new HotChocolate.Language.ListValueNode(items);

        for (var i = 0; i < depth; i++)
        {
            value = new HotChocolate.Language.ObjectValueNode(
                new ObjectFieldNode("value", value));
        }

        return value;
    }

    private void InitializeStore(FetchResultStore store, MemoryArena arena)
        => store.Initialize(
            arena,
            _schema,
            DefaultErrorHandler.Default,
            _operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

    private static OperationRequirement Requirement(string key)
        => new(
            key,
            new NamedTypeNode("String"),
            SelectionPath.Root,
            new PathNode(new PathSegmentNode(new FusionNameNode(key))),
            null);

    private static CompactPath Path(params int[] segments)
    {
        var buffer = new int[segments.Length + 1];
        buffer[0] = segments.Length;
        segments.CopyTo(buffer.AsSpan(1));
        return new CompactPath(buffer);
    }
}
