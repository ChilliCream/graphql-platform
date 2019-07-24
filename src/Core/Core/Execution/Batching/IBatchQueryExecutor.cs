using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Batching
{
    public interface IBatchQueryExecutor
        : IDisposable
    {
        ISchema Schema { get; }

        Task<IBatchQueryExecutionResult> ExecuteAsync(
            IReadOnlyList<IReadOnlyQueryRequest> batch,
            CancellationToken cancellationToken);
    }
}
