using System.IO.Pipelines;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class RawOperationResult : ExecutionResult, IRawJsonSerializer, IOperationResult
{
    private readonly CompositeResultDocument _result;
    private readonly IReadOnlyDictionary<string, object?>? _contextData;

    internal RawOperationResult(
        CompositeResultDocument result,
        IReadOnlyDictionary<string, object?>? contextData)
    {
        _result = result;
        _contextData = contextData;
    }

    public override ExecutionResultKind Kind => ExecutionResultKind.SingleResult;

    public CompositeResultDocument Result => _result;

    public override IReadOnlyDictionary<string, object?>? ContextData => _contextData;

    public void WriteTo(PipeWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _result.WriteTo(writer);
    }

    #region  NotSupported

    int? IOperationResult.RequestIndex => throw new NotSupportedException();

    int? IOperationResult.VariableIndex => throw new NotSupportedException();

    string? IOperationResult.Label => throw new NotSupportedException();

    Path? IOperationResult.Path => throw new NotSupportedException();

    IReadOnlyDictionary<string, object?>? IOperationResult.Data => throw new NotSupportedException();

    IReadOnlyList<object?>? IOperationResult.Items => throw new NotSupportedException();

    IReadOnlyList<IError>? IOperationResult.Errors => throw new NotSupportedException();

    IReadOnlyDictionary<string, object?>? IOperationResult.Extensions => throw new NotSupportedException();

    IReadOnlyList<IOperationResult>? IOperationResult.Incremental => throw new NotSupportedException();

    bool? IOperationResult.HasNext => throw new NotSupportedException();

    bool IOperationResult.IsDataSet => throw new NotSupportedException();

    #endregion
}
