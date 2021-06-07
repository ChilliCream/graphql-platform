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
    }
}
