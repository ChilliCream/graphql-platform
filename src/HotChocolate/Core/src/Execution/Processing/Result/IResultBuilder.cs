using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution.Processing;

public interface IResultBuilder
{
    public IReadOnlyList<IError> Errors { get; }

    public void SetData(ObjectResult? data);

    public void SetItems(IReadOnlyList<object?> items);

    public void SetExtension(string key, object? value);

    public void SetExtension<T>(string key, UpdateState<T> value);

    public void SetExtension<T, TState>(string key, TState state, UpdateState<T, TState> value);

    public void SetContextData(string key, object? value);

    public void SetContextData(string key, UpdateState<object?> value);

    public void SetContextData<TState>(string key, TState state, UpdateState<object?, TState> value);

    /// <summary>
    /// Register cleanup tasks that will be executed after resolver execution is finished.
    /// </summary>
    /// <param name="action">
    /// Cleanup action.
    /// </param>
    public void RegisterForCleanup(Func<ValueTask> action);

    public void RegisterForCleanup<T>(T state, Func<T, ValueTask> action);

    public void RegisterForCleanup<T>(T state) where T : IDisposable;

    public void SetPath(Path? path);

    public void SetLabel(string? label);

    public void SetHasNext(bool value);

    public void SetSingleErrorPerPath(bool value = true);

    public void AddError(IError error, ISelection? selection = null);

    public void AddNonNullViolation(ISelection selection, Path path);

    public void AddRemovedResult(ResultData result);

    public void AddPatchId(uint patchId);

    public void SetRequestIndex(int requestIndex);

    public void SetVariableIndex(int variableIndex);

    public IOperationResult BuildResult();

    public void DiscardResult();

    public ObjectResult RentObject(int capacity);

    public ListResult RentList(int capacity);

    public void Initialize(RequestContext context, IExecutionDiagnosticEvents diagnosticEvents);

    public void Clear();
}
