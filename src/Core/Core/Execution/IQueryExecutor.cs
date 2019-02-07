using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IQueryExecutor
        : IDisposable
    {
        ISchema Schema { get; }

        Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
            CancellationToken cancellationToken);
    }
}
