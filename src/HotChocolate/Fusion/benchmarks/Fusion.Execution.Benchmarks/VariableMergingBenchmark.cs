using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using FusionNameNode = HotChocolate.Fusion.Language.NameNode;

namespace Fusion.Execution.Benchmarks;

/// <summary>
/// Compares the cost of the defer-only snapshot variable merge path
/// (<see cref="FetchResultStore.CreateVariableValueSetsFromSnapshot"/>) against
/// the existing non-defer variable creation path. The wholesale Case D return
/// is included to confirm it stays allocation-free.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class VariableMergingBenchmark
{
    private static readonly IReadOnlyList<ObjectFieldNode> s_oneForwardedVariable =
        [new ObjectFieldNode("limit", new IntValueNode(10))];

    private static readonly IReadOnlyList<ObjectFieldNode> s_noForwardedVariables = [];

    private static readonly OperationRequirement[] s_singleRequirement =
        [Requirement("__fusion_1_id")];

    private static readonly HashSet<string> s_oneImportedKey =
        new(["__fusion_1_id"], StringComparer.Ordinal);

    private static readonly HashSet<string> s_twoImportedKeys =
        new(["__fusion_1_id", "__fusion_2_sku"], StringComparer.Ordinal);

    private FetchResultStore _baselineStore = null!;
    private FetchResultStore _snapshotStore = null!;

    private ImmutableArray<VariableValues> _singleEntrySnapshot;
    private ImmutableArray<VariableValues> _subsetEntrySnapshot;
    private ImmutableArray<VariableValues> _list10Snapshot;
    private ImmutableArray<VariableValues> _list100Snapshot;
    private ImmutableArray<VariableValues> _list1000Snapshot;

    private ImmutableArray<VariableValues> _caseDSnapshot;

    [GlobalSetup]
    public void Setup()
    {
        _baselineStore = new FetchResultStore();
        _snapshotStore = new FetchResultStore();

        // The "source" store mints VariableValues entries that the snapshot
        // merge consumes as imported parent rows. A separate store mirrors how
        // a deferred sub-plan imports values from its parent before resolving.
        var source = new FetchResultStore();

        _singleEntrySnapshot =
        [
            CreateImportedEntry(
                source,
                CompactPath.Root,
                Field("__fusion_1_id", new StringValueNode("1")))
        ];

        _subsetEntrySnapshot =
        [
            CreateImportedEntry(
                source,
                CompactPath.Root,
                Field("__fusion_1_id", new StringValueNode("1")),
                Field("__fusion_2_sku", new StringValueNode("sku-1")))
        ];

        _list10Snapshot = BuildListSnapshot(source, count: 10);
        _list100Snapshot = BuildListSnapshot(source, count: 100);
        _list1000Snapshot = BuildListSnapshot(source, count: 1000);

        // Case D returns the full imported snapshot wholesale. Use a populated
        // entry so the field read is the only cost being measured.
        _caseDSnapshot = _singleEntrySnapshot;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _baselineStore.Dispose();
        _snapshotStore.Dispose();
    }

    /// <summary>
    /// Existing non-defer per-entry write path through the same
    /// <see cref="FetchResultStore"/> writer used by the general overloads.
    /// Acts as the per-entry baseline against which the snapshot merge cost is
    /// compared. The full non-defer overload also walks the result document via
    /// CollectTargetElements, which depends on a populated CompositeResultDocument
    /// and is intentionally outside the scope of this benchmark.
    /// </summary>
    [Benchmark(Baseline = true)]
    public VariableValues NonDefer_Baseline()
        => _baselineStore.CreateVariableValueSets(CompactPath.Root, s_oneForwardedVariable);

    /// <summary>
    /// Case D: all requested keys are imported, no forwarded variables, and
    /// the requested set equals the imported set. The runtime returns the
    /// imported snapshot wholesale, so the only cost is the field read.
    /// </summary>
    [Benchmark]
    public ImmutableArray<VariableValues> Defer_CaseD_WholesaleReturn()
        => _caseDSnapshot;

    /// <summary>
    /// Case E with one forwarded variable and one requested requirement.
    /// </summary>
    [Benchmark]
    public ImmutableArray<VariableValues> Defer_Snapshot_OneForwardedOneRequirement()
        => _snapshotStore.CreateVariableValueSetsFromSnapshot(
            _singleEntrySnapshot,
            s_oneImportedKey,
            s_oneForwardedVariable,
            s_singleRequirement);

    /// <summary>
    /// Case E strict subset filter. The imported snapshot carries two keys but
    /// only one is requested, so the merge writes a smaller variable object.
    /// </summary>
    [Benchmark]
    public ImmutableArray<VariableValues> Defer_Snapshot_StrictSubset()
        => _snapshotStore.CreateVariableValueSetsFromSnapshot(
            _subsetEntrySnapshot,
            s_twoImportedKeys,
            s_noForwardedVariables,
            s_singleRequirement);

    [Benchmark]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N10()
        => _snapshotStore.CreateVariableValueSetsFromSnapshot(
            _list10Snapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            s_singleRequirement);

    [Benchmark]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N100()
        => _snapshotStore.CreateVariableValueSetsFromSnapshot(
            _list100Snapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            s_singleRequirement);

    [Benchmark]
    public ImmutableArray<VariableValues> Defer_Snapshot_List_N1000()
        => _snapshotStore.CreateVariableValueSetsFromSnapshot(
            _list1000Snapshot,
            s_oneImportedKey,
            s_noForwardedVariables,
            s_singleRequirement);

    private static ImmutableArray<VariableValues> BuildListSnapshot(
        FetchResultStore source,
        int count)
    {
        var builder = ImmutableArray.CreateBuilder<VariableValues>(count);

        for (var i = 0; i < count; i++)
        {
            builder.Add(
                CreateImportedEntry(
                    source,
                    Path(i),
                    Field("__fusion_1_id", new StringValueNode(i.ToString()))));
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

    private static OperationRequirement Requirement(string key)
        => new(
            key,
            new NamedTypeNode("String"),
            SelectionPath.Root,
            new PathNode(new PathSegmentNode(new FusionNameNode(key))));

    private static CompactPath Path(params int[] segments)
    {
        var buffer = new int[segments.Length + 1];
        buffer[0] = segments.Length;
        segments.CopyTo(buffer.AsSpan(1));
        return new CompactPath(buffer);
    }
}
