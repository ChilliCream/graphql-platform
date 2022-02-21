using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Execution;

/// <summary>
/// This remote request scheduler allows to enqueue a request to a remote
/// GraphQL server for execution. The scheduler will if possible batch multiple requests at once.
/// </summary>
public interface IRemoteRequestScheduler : IDisposable
{
    /// <summary>
    /// Gets the remote schema representation
    /// </summary>
    ISchema Schema { get; }

    /// <summary>
    /// Gets the underlying request executor.
    /// </summary>
    IRequestExecutor Executor { get; }

    /// <summary>
    /// Schedules the given GraphQL <paramref name="request" />.
    /// </summary>
    /// <param name="request">
    /// The GraphQL request object.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a promise for the execution result of the given GraphQL <paramref name="request" />.
    ///
    /// If the request operation is a simple query or mutation the result is a
    /// <see cref="IQueryResult" />.
    ///
    /// If the request operation is a query or mutation where data is deferred, streamed or
    /// includes live data the result is a <see cref="IResponseStream" /> where each result
    /// that the <see cref="IResponseStream" /> yields is a <see cref="IQueryResult" />.
    ///
    /// If the request operation is a subscription the result is a
    /// <see cref="IResponseStream" /> where each result that the
    /// <see cref="IResponseStream" /> yields is a
    /// <see cref="IReadOnlyQueryResult" />.
    /// </returns>
    Task<IExecutionResult> ScheduleAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default);
}
