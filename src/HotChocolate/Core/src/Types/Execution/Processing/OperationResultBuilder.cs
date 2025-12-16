using System.Collections.Immutable;
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

    public ImmutableList<IError> Errors { get; set; } = ImmutableList<IError>.Empty;

    public ImmutableDictionary<string, object?> Extensions { get; set; } = ImmutableDictionary<string, object?>.Empty;

    public ImmutableDictionary<string, object?> ContextData { get; set; } = ImmutableDictionary<string, object?>.Empty;

    public ImmutableList<PendingResult>? Pending { get; set; }

    public ImmutableList<IIncrementalResult>? Incremental { get; set; }

    public ImmutableList<CompletedResult>? Completed { get; set; }

    public ImmutableList<Func<ValueTask>> CleanupTasks { get; set; } = ImmutableList<Func<ValueTask>>.Empty;

    // TODO : Is this still needed?
    public ImmutableHashSet<Path> NonNullViolations { get; set; } = ImmutableHashSet<Path>.Empty;

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

    public void SetExtension<TValue>(string key, UpdateState<TValue> value)
    {
        lock (_sync)
        {
            if (Extensions.TryGetValue(key, out var currentValue))
            {
                var newValue = value(key, (TValue)currentValue!);
                Extensions = Extensions.SetItem(key, newValue);
            }
            else
            {
                var initialValue = value(key, default!);
                Extensions = Extensions.Add(key, initialValue);
            }
        }
    }

    public void SetExtension<TValue, TState>(string key, TState state, UpdateState<TValue, TState> value)
    {
        lock (_sync)
        {
            if (Extensions.TryGetValue(key, out var currentValue))
            {
                var newValue = value(key, (TValue)currentValue!, state);
                Extensions = Extensions.SetItem(key, newValue);
            }
            else
            {
                var initialValue = value(key, default!, state);
                Extensions = Extensions.Add(key, initialValue);
            }
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
            if (Extensions.TryGetValue(key, out var currentValue))
            {
                var newValue = value(key, currentValue);
                Extensions = Extensions.SetItem(key, newValue);
            }
            else
            {
                var initialValue = value(key, null);
                Extensions = Extensions.Add(key, initialValue);
            }
        }
    }

    public void SetResultState<TState>(string key, TState state, UpdateState<object?, TState> value)
    {
        lock (_sync)
        {
            if (Extensions.TryGetValue(key, out var currentValue))
            {
                var newValue = value(key, currentValue, state);
                Extensions = Extensions.SetItem(key, newValue);
            }
            else
            {
                var initialValue = value(key, null, state);
                Extensions = Extensions.Add(key, initialValue);
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
        Errors = ImmutableList<IError>.Empty;
        Pending = ImmutableList<PendingResult>.Empty;
        Extensions = ImmutableDictionary<string, object?>.Empty;
        CleanupTasks  = ImmutableList<Func<ValueTask>>.Empty;
        NonNullViolations = ImmutableHashSet<Path>.Empty;
        Pending = null;
        Incremental = null;
        Completed = null;
        HasNext = null;
    }
}
