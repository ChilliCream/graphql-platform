using HotChocolate.Resolvers;

namespace HotChocolate.Execution.ExecutionGroups;

/// <summary>
/// Records resolver events and holds resolvers open until the test releases them.
/// </summary>
internal sealed class ExecutionScript
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);
    private readonly object _sync = new();
    private readonly List<string> _events = [];
    private readonly Dictionary<string, TaskCompletionSource> _gates = [];
    private readonly Dictionary<string, TaskCompletionSource> _started = [];
    private readonly HashSet<string> _nulls = [];

    public IReadOnlyList<string> Events
    {
        get
        {
            lock (_sync)
            {
                return [.. _events];
            }
        }
    }

    public void Release(string path) => GetGate(path).TrySetResult();

    public void Fail(string path)
        => GetGate(path).TrySetException(new InvalidOperationException($"The resolver at {path} failed."));

    public void ReturnNull(string path)
    {
        lock (_sync)
        {
            _nulls.Add(path);
        }
    }

    public void Cancel(string path) => GetGate(path).TrySetCanceled();

    public void Reached(string name) => GetStarted(name).TrySetResult();

    public Task Started(string path) => GetStarted(path).Task.WaitAsync(s_timeout);

    public void Log(string text)
    {
        lock (_sync)
        {
            _events.Add(text);
        }
    }

    public async ValueTask<object?> RunAsync(IResolverContext context, object? value)
    {
        var path = context.Path.ToString();

        Log($"start {path}");
        GetStarted(path).TrySetResult();

        try
        {
            await GetGate(path).Task.WaitAsync(context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            Log($"cancel {path}");
            throw;
        }
        catch
        {
            Log($"fail {path}");
            throw;
        }

        Log($"end {path}");

        lock (_sync)
        {
            return _nulls.Contains(path) ? null : value;
        }
    }

    public ValueTask<object?> Resolve(IResolverContext context, object? value)
    {
        var path = context.Path.ToString();
        GetStarted(path).TrySetResult();

        lock (_sync)
        {
            return new ValueTask<object?>(_nulls.Contains(path) ? null : value);
        }
    }

    private TaskCompletionSource GetGate(string path)
    {
        lock (_sync)
        {
            if (!_gates.TryGetValue(path, out var gate))
            {
                gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _gates.Add(path, gate);
            }

            return gate;
        }
    }

    private TaskCompletionSource GetStarted(string path)
    {
        lock (_sync)
        {
            if (!_started.TryGetValue(path, out var started))
            {
                started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _started.Add(path, started);
            }

            return started;
        }
    }
}
