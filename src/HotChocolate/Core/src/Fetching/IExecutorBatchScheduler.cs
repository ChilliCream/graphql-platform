using System;
using System.Threading;
using GreenDonut;

namespace HotChocolate.Fetching;

/// <summary>
/// Represents the GraphQL batch dispatcher.
/// </summary>
public interface IExecutorBatchScheduler : IBatchScheduler, IDisposable
{
    /// <summary>
    /// Register a callback that is invoked when a task is enqueued.
    /// </summary>
    /// <param name="callback">
    /// The callback that is invoked when a task is enqueued.
    /// </param>
    void RegisterTaskEnqueuedCallback(Action callback);

    /// <summary>
    /// Begins dispatching batched jobs.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token that can be used to cancel the dispatching.
    /// </param>
    void BeginDispatch(CancellationToken cancellationToken = default);
}