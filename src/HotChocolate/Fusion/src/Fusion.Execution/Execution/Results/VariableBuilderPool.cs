namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Per-store pool of <see cref="VariableBuilder"/> instances. Uses a dedicated lock
/// separate from <see cref="FetchResultStore"/>'s result-mutation lock so rent/return
/// does not contend with result integration.
/// </summary>
internal sealed class VariableBuilderPool : IDisposable
{
    private const int MaxRetained = 16;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly Stack<VariableBuilder> _pool = new();
    private bool _disposed;

    public VariableBuilder Rent()
    {
        lock (_lock)
        {
            if (_pool.TryPop(out var builder))
            {
                return builder;
            }
        }

        return new VariableBuilder();
    }

    public void Return(VariableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        lock (_lock)
        {
            if (_disposed)
            {
                builder.Dispose();
                return;
            }

            _pool.Push(builder);
        }
    }

    public void Clean()
    {
        lock (_lock)
        {
            while (_pool.Count > MaxRetained)
            {
                _pool.Pop().Dispose();
            }

            foreach (var builder in _pool)
            {
                builder.Clean();
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            while (_pool.TryPop(out var builder))
            {
                builder.Dispose();
            }
        }
    }
}
