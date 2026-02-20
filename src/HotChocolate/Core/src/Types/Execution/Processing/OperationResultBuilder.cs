using System.Collections.Immutable;
using HotChocolate.Collections.Immutable;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

internal sealed class OperationResultBuilder : IOperationResultBuilder
{
    private readonly object _sync = new();

    public int RequestIndex { get; set; } = -1;

    public int VariableIndex { get; set; } = -1;

    public Path? Path { get; set; }

    public ResultDocument Data { get; set; } = null!;

    public ImmutableList<IError> Errors { get; set; } = [];

    public ImmutableOrderedDictionary<string, object?> Extensions { get; set; } = [];

    public ImmutableDictionary<string, object?> ContextData { get; set; } = ImmutableDictionary<string, object?>.Empty;

    public ImmutableList<PendingResult> Pending { get; set; } = [];

    public ImmutableList<IIncrementalResult> Incremental { get; set; } = [];

    public ImmutableList<CompletedResult> Completed { get; set; } = [];

    public ImmutableList<Func<ValueTask>> CleanupTasks { get; set; } = [];

    public ImmutableHashSet<Path> NonNullViolations { get; set; } = [];

    public bool? HasNext { get; set; }

    public void AddError(IError error)
    {
        lock (_sync)
        {
            Errors = Errors.Add(error);
        }
    }

    public void AddErrorRange(IReadOnlyList<IError> errors)
    {
        lock (_sync)
        {
            Errors = Errors.AddRange(errors);
        }
    }

    public void AddNonNullViolation(Path path)
    {
        lock (_sync)
        {
            NonNullViolations = NonNullViolations.Add(path);
        }
    }

    public void SetExtension<TValue>(string key, TValue value)
    {
        lock (_sync)
        {
            Extensions = Extensions.SetItem(key, value);
        }
    }

    public void SetResultState(string key, object? value)
    {
        lock (_sync)
        {
            ContextData = ContextData.SetItem(key, value);
        }
    }

    public void SetResultState(string key, UpdateState<object?> value)
    {
        lock (_sync)
        {
            if (ContextData.TryGetValue(key, out var currentValue))
            {
                var newValue = value(key, currentValue);
                ContextData = ContextData.SetItem(key, newValue);
            }
            else
            {
                var initialValue = value(key, null);
                ContextData = ContextData.Add(key, initialValue);
            }
        }
    }

    public void SetResultState<TState>(string key, TState state, UpdateState<object?, TState> value)
    {
        lock (_sync)
        {
            if (ContextData.TryGetValue(key, out var currentValue))
            {
                var newValue = value(key, currentValue, state);
                ContextData = ContextData.SetItem(key, newValue);
            }
            else
            {
                var initialValue = value(key, null, state);
                ContextData = ContextData.Add(key, initialValue);
            }
        }
    }

    public void RegisterForCleanup(Func<ValueTask> action)
    {
        lock (_sync)
        {
            CleanupTasks = CleanupTasks.Add(action);
        }
    }

    public void Reset()
    {
        RequestIndex = -1;
        VariableIndex = -1;
        Path = null;
        Data = null!;
        Errors = [];
        Extensions = [];
        ContextData = ImmutableDictionary<string, object?>.Empty;
        CleanupTasks = [];
        NonNullViolations = [];
        Pending = [];
        Incremental = [];
        Completed = [];
        HasNext = null;
    }
}
