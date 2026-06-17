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
        int count)
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
    }

    public ExecutionNode Node { get; }

    public string SchemaName { get; }

    public SelectionPath SourcePath { get; }

    public ResultSelectionSet ResultSelectionSet { get; }

    public ImmutableArray<VariableValues> VariableValueSets { get; }

    public bool ContainsErrors { get; }

    public static PendingMerge Empty(
        ExecutionNode node,
        string schemaName,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        ImmutableArray<VariableValues> variableValueSets,
        bool containsErrors)
        => new(
            node,
            schemaName,
            sourcePath,
            resultSelectionSet,
            variableValueSets,
            containsErrors,
            PendingMergeKind.Empty,
            result: null,
            buffer: null,
            count: 0);

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
            case PendingMergeKind.Empty:
                context.AddPartialResults(SourcePath, [], ResultSelectionSet, ContainsErrors);
                break;

            case PendingMergeKind.Single:
                context.AddPartialResult(SourcePath, _result!, ResultSelectionSet, ContainsErrors);
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
        Empty,
        Single,
        Multiple
    }
}
