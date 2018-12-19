using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IResponseStream
        : IDisposable
    {
        /// <summary>
        /// Defines if this stream is completed.
        /// A completed response stream does not yield any new results.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Reads the next result from the current response stream.
        /// </summary>
        Task<IQueryExecutionResult> ReadAsync();

        /// <summary>
        /// Reads the next result from the current response stream.
        /// </summary>
        Task<IQueryExecutionResult> ReadAsync(
            CancellationToken cancellationToken);
    }
}
