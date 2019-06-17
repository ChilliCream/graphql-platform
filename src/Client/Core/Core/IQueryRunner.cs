using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// Runns a potentially paged query.
    /// </summary>
    public interface IQueryRunner
    {
        /// <summary>
        /// Gets the result of the query.
        /// </summary>
        object Result { get; }

        /// <summary>
        /// Runs the next page of the query.
        /// </summary>
        /// <param name="cancellationToken">The optional cancellation token to use.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that returns whether a page was returned.
        /// </returns>
        Task<bool> RunPage(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Runns a potentially paged query.
    /// </summary>
    public interface IQueryRunner<out TResult> : IQueryRunner
    {
        /// <summary>
        /// Gets the result of the query.
        /// </summary>
        new TResult Result { get; }
    }
}
