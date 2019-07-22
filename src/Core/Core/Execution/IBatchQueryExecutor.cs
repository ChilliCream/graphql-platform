using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IBatchQueryExecutor
        : IDisposable
    {
        ISchema Schema { get; }

        Task<IBatchExecutionResult> ExecuteAsync(
            IReadOnlyList<IReadOnlyQueryRequest> batch,
            CancellationToken cancellationToken);
    }
}
