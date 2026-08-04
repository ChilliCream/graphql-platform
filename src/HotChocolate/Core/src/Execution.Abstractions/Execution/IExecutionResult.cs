using System.Collections.Immutable;
using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the result of the GraphQL execution pipeline.
/// </summary>
/// <remarks>
/// Execution results are by default disposable, and disposing
/// them allows it to give back its used memory to the execution
/// engine result pools.
/// </remarks>
public interface IExecutionResult : IFeatureProvider, IAsyncDisposable
{
    /// <summary>
    /// Gets the result kind.
    /// </summary>
    ExecutionResultKind Kind { get; }

    /// <summary>
    /// Gets the result context data which represent additional
    /// properties that are NOT written to the transport.
    /// </summary>
    ImmutableDictionary<string, object?> ContextData
    {
        get => Features.Get<ImmutableDictionary<string, object?>>() ?? ImmutableDictionary<string, object?>.Empty;
        set => Features.Set(value);
    }

    /// <summary>
    /// Registers a resource that needs to be disposed when the result is being disposed.
    /// </summary>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    void RegisterForCleanup(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        RegisterForCleanup(() =>
        {
            disposable.Dispose();
            return default;
        });
    }

    /// <summary>
    /// Registers a cleanup action to be executed when the result is disposed.
    /// </summary>
    /// <param name="clean">
    /// A cleanup action that will be executed when this result is disposed.
    /// </param>
    void RegisterForCleanup(Action clean)
    {
        ArgumentNullException.ThrowIfNull(clean);
        RegisterForCleanup(() =>
        {
            clean();
            return default;
        });
    }

    /// <summary>
    /// Registers a resource that needs to be disposed asynchronously when the result is being disposed.
    /// </summary>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    void RegisterForCleanup(IAsyncDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        RegisterForCleanup(disposable.DisposeAsync);
    }

    /// <summary>
    /// Registers a cleanup task for execution resources bound to this execution result.
    /// </summary>
    /// <param name="clean">
    /// A cleanup task that will be executed when this result is disposed.
    /// </param>
    void RegisterForCleanup(Func<ValueTask> clean);
}
