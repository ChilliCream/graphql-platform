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
    /// Registers a cleanup task for execution resources bound to this execution result.
    /// </summary>
    /// <param name="clean">
    /// A cleanup task that will be executed when this result is disposed.
    /// </param>
    void RegisterForCleanup(Func<ValueTask> clean);
}
