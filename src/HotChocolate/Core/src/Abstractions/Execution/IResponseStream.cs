using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// The response stream represents a stream of <see cref="IQueryResult" /> that are produced
    /// by the execution engine.
    /// </summary>
    public interface IResponseStream : IAsyncDisposable
    {
        /// <summary>
        /// Reads the subscription results from the execution engine.
        /// </summary>
        IAsyncEnumerable<IQueryResult> ReadResultsAsync();

        /// <summary>
        /// Registers disposable dependencies that have to be disposed when this stream disposes.
        /// </summary>
        /// <param name="disposable">
        /// The disposable dependency that needs to be disposed with this stream.
        /// </param>
        void RegisterDisposable(IDisposable disposable);
    }
}
