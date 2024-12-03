using HotChocolate.Properties;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a operation result object.
/// </summary>
public sealed class OperationResult : ExecutionResult, IOperationResult
{
    internal OperationResult(
        IReadOnlyDictionary<string, object?>? data,
        IReadOnlyList<IError>? errors,
        IReadOnlyDictionary<string, object?>? extensions,
        IReadOnlyDictionary<string, object?>? contextData,
        IReadOnlyList<object?>? items,
        IReadOnlyList<IOperationResult>? incremental,
        string? label,
        Path? path,
        bool? hasNext,
        Func<ValueTask>[] cleanupTasks,
        bool isDataSet,
        int? requestIndex,
        int? variableIndex,
        bool skipValidation = false)
        : base(cleanupTasks)
    {
        if (!skipValidation &&
            data is null &&
            items is null &&
            errors is null &&
            incremental is null &&
            hasNext is not false)
        {
            throw new ArgumentException(
                AbstractionResources.QueryResult_DataAndResultAreNull,
                nameof(data));
        }

        Data = data;
        Items = items;
        Errors = errors;
        Extensions = extensions;
        ContextData = contextData;
        Incremental = incremental;
        Label = label;
        Path = path;
        HasNext = hasNext;
        IsDataSet = isDataSet;
        RequestIndex = requestIndex;
        VariableIndex = variableIndex;
    }

    /// <summary>
    /// Initializes a new <see cref="OperationResult"/>.
    /// </summary>
    public OperationResult(
        IReadOnlyDictionary<string, object?>? data,
        IReadOnlyList<IError>? errors = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IReadOnlyList<object?>? items = null,
        IReadOnlyList<IOperationResult>? incremental = null,
        string? label = null,
        Path? path = null,
        bool? hasNext = null,
        int? requestIndex = null,
        int? variableIndex = null)
    {
        if (data is null &&
            items is null &&
            errors is null &&
            incremental is null &&
            hasNext is not false)
        {
            throw new ArgumentException(
                AbstractionResources.QueryResult_DataAndResultAreNull,
                nameof(data));
        }

        Data = data;
        Items = items;
        Errors = errors;
        Extensions = extensions;
        ContextData = contextData;
        Incremental = incremental;
        Label = label;
        Path = path;
        HasNext = hasNext;
        RequestIndex = requestIndex;
        VariableIndex = variableIndex;
    }

    /// <inheritdoc />
    public override ExecutionResultKind Kind => ExecutionResultKind.SingleResult;

    /// <inheritdoc />
    public int? RequestIndex { get; }

    /// <inheritdoc />
    public int? VariableIndex { get; }

    /// <inheritdoc />
    public string? Label { get; }

    /// <inheritdoc />
    public Path? Path { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Data { get; }

    /// <inheritdoc />
    public IReadOnlyList<object?>? Items { get; }

    /// <inheritdoc />
    public IReadOnlyList<IError>? Errors { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <inheritdoc />
    public IReadOnlyList<IOperationResult>? Incremental { get; }

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <inheritdoc />
    public bool? HasNext { get; }

    /// <inheritdoc />
    public bool IsDataSet { get; }

    /// <summary>
    /// Creates a new <see cref="OperationResult"/> with the specified extension data.
    /// </summary>
    /// <param name="extensions">
    /// The extension data that shall be added to the result.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="OperationResult"/> that represents the result.
    /// </returns>
    public OperationResult WithExtensions(
        IReadOnlyDictionary<string, object?>? extensions)
        => new OperationResult(
            data: Data,
            errors: Errors,
            extensions: extensions,
            contextData: ContextData,
            items: Items,
            incremental: Incremental,
            label: Label,
            path: Path,
            hasNext: HasNext,
            cleanupTasks: CleanupTasks,
            isDataSet: IsDataSet,
            requestIndex: RequestIndex,
            variableIndex: VariableIndex);

    /// <summary>
    /// Creates a new <see cref="OperationResult"/> with the specified context data.
    /// </summary>
    /// <param name="contextData">
    /// The context data that shall be added to the result.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="OperationResult"/> that represents the result.
    /// </returns>
    public OperationResult WithContextData(
        IReadOnlyDictionary<string, object?>? contextData)
        => new OperationResult(
            data: Data,
            errors: Errors,
            extensions: Extensions,
            contextData: contextData,
            items: Items,
            incremental: Incremental,
            label: Label,
            path: Path,
            hasNext: HasNext,
            cleanupTasks: CleanupTasks,
            isDataSet: IsDataSet,
            requestIndex: RequestIndex,
            variableIndex: VariableIndex);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> ToDictionary()
        => OperationResultHelper.ToDictionary(this);
}
