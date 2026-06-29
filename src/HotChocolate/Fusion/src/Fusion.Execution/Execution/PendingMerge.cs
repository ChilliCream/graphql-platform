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
        bool propagateNull)
    {
        Node = node;
        SchemaName = schemaName;
        SourcePath = sourcePath;
        ResultSelectionSet = resultSelectionSet;
        VariableValueSets = variableValueSets;
        ContainsErrors = containsErrors;
        PropagateNull = propagateNull;
        _kind = kind;
        _result = result;
        _buffer = buffer;
        _count = count;
    }

    public ExecutionNode Node { get; }

    public string SchemaName { get; }

    public SelectionPath SourcePath { get; }

    public ResultSelectionSet ResultSelectionSet { get; }

    public ImmutableArray<VariableValues> VariableValueSets { get; }

    public bool ContainsErrors { get; }

    public bool PropagateNull { get; }

    public static PendingMerge Single(
        ExecutionNode node,
        string schemaName,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        ImmutableArray<VariableValues> variableValueSets,
        SourceSchemaResult result,
        bool containsErrors,
        bool propagateNull)
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
            count: 1,
            propagateNull);

    public static PendingMerge Multiple(
        ExecutionNode node,
        string schemaName,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        ImmutableArray<VariableValues> variableValueSets,
        SourceSchemaResult[] buffer,
        int count,
        bool containsErrors,
        bool propagateNull)
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
            count,
            propagateNull);

    public void Apply(OperationPlanContext context)
    {
        switch (_kind)
        {
            case PendingMergeKind.Single:
                context.AddPartialResult(
                    SourcePath,
                    _result!,
                    ResultSelectionSet,
                    ContainsErrors,
                    PropagateNull);
                break;

            case PendingMergeKind.Multiple:
                var buffer = _buffer!;
                try
                {
                    context.AddPartialResults(
                        SourcePath,
                        buffer.AsSpan(0, _count),
                        ResultSelectionSet,
                        ContainsErrors,
                        PropagateNull);
                }
                finally
                {
                    buffer.AsSpan(0, _count).Clear();
                    ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
                }
                break;
        }
    }

    public void DisposeUnmerged()
    {
        switch (_kind)
        {
            case PendingMergeKind.Single:
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
        Multiple
    }
}
