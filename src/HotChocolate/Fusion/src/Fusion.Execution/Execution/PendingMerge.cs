using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

internal readonly struct PendingMerge
{
    private readonly PendingMergeKind _kind;
    private readonly SourceSchemaResult? _result;
    private readonly SourceSchemaResult[]? _buffer;
    private readonly int _count;
    private readonly RepresentationValue _representation;

    private PendingMerge(
        ExecutionNode node,
        string schemaName,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        ImmutableArray<VariableValues> variableValueSets,
        bool containsErrors,
        PendingMergeKind kind,
        SourceSchemaResult? result,
        SourceSchemaResult[]? buffer,
        int count,
        RepresentationValue representation = default)
    {
        Node = node;
        SchemaName = schemaName;
        SourcePath = sourcePath;
        ResultSelectionSet = resultSelectionSet;
        VariableValueSets = variableValueSets;
        ContainsErrors = containsErrors;
        _kind = kind;
        _result = result;
        _buffer = buffer;
        _count = count;
        _representation = representation;
    }

    public ExecutionNode Node { get; }

    public string SchemaName { get; }

    public SelectionPath SourcePath { get; }

    public ResultSelectionSet ResultSelectionSet { get; }

    public ImmutableArray<VariableValues> VariableValueSets { get; }

    public bool ContainsErrors { get; }

    public static PendingMerge Single(
        ExecutionNode node,
        string schemaName,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        ImmutableArray<VariableValues> variableValueSets,
        SourceSchemaResult result,
        bool containsErrors)
        => new(
            node,
            schemaName,
            sourcePath,
            resultSelectionSet,
            variableValueSets,
            containsErrors,
            PendingMergeKind.Single,
            result,
            buffer: null,
            count: 1);

    public static PendingMerge RepresentationSingle(
        ExecutionNode node,
        string schemaName,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        ImmutableArray<VariableValues> variableValueSets,
        RepresentationValue representation,
        SourceSchemaResult result,
        bool containsErrors)
        => new(
            node,
            schemaName,
            sourcePath,
            resultSelectionSet,
            variableValueSets,
            containsErrors,
            PendingMergeKind.Representation,
            result,
            buffer: null,
            count: 1,
            representation);

    public static PendingMerge Multiple(
        ExecutionNode node,
        string schemaName,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        ImmutableArray<VariableValues> variableValueSets,
        SourceSchemaResult[] buffer,
        int count,
        bool containsErrors)
        => new(
            node,
            schemaName,
            sourcePath,
            resultSelectionSet,
            variableValueSets,
            containsErrors,
            PendingMergeKind.Multiple,
            result: null,
            buffer,
            count);

    public void Apply(OperationPlanContext context)
    {
        switch (_kind)
        {
            case PendingMergeKind.Single:
                context.AddPartialResult(SourcePath, _result!, ResultSelectionSet, ContainsErrors);
                break;

            case PendingMergeKind.Representation:
                context.AddRepresentationResult(
                    SourcePath,
                    _result!,
                    _representation,
                    ResultSelectionSet,
                    ContainsErrors);
                break;

            case PendingMergeKind.Multiple:
                var buffer = _buffer!;
                try
                {
                    context.AddPartialResults(
                        SourcePath,
                        buffer.AsSpan(0, _count),
                        ResultSelectionSet,
                        ContainsErrors);
                }
                finally
                {
                    buffer.AsSpan(0, _count).Clear();
                    ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
                }
                break;
        }
    }

    /// <summary>
    /// Reports the given merge failure against every result path this merge
    /// would have populated.
    /// </summary>
    public void AddErrors(OperationPlanContext context, Exception exception)
        => context.AddErrors(exception, VariableValueSets, ResultSelectionSet);

    public void DisposeUnmerged()
    {
        switch (_kind)
        {
            case PendingMergeKind.Single:
                _result?.Dispose();
                break;

            case PendingMergeKind.Representation:
                // TODO(apollo-representations): the successful merge path
                // (FetchResultStore.AddRepresentationResult) returns the representation's pooled
                // CompactPath segments to the store's local path pool via
                // ReturnRepresentationPathSegments(_representation) in its finally block. On this
                // cancel/error path those segments (every EntityResultPath.Path and its
                // AdditionalPaths in _representation.ResultPaths) are not returned to the pool and
                // are only reclaimed later when the FetchResultStore is disposed. Returning them
                // here requires a handle to the FetchResultStore so the segments can be returned
                // under its lock using _pathPool and _seenPaths. PendingMerge does not carry that
                // handle, and DisposeUnmerged also runs from ExecutionState.ClearPendingMerges
                // where no OperationPlanContext or FetchResultStore is available. Plumbing a store
                // handle through PendingMerge belongs to the broader in-flight execution refactor.
                _result?.Dispose();
                break;

            case PendingMergeKind.Multiple:
                var buffer = _buffer;
                if (buffer is not null)
                {
                    foreach (var result in buffer.AsSpan(0, _count))
                    {
                        result?.Dispose();
                    }

                    buffer.AsSpan(0, _count).Clear();
                    ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
                }
                break;
        }
    }

    private enum PendingMergeKind
    {
        Single,
        Representation,
        Multiple
    }
}
