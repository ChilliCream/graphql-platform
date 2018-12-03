using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IQueryExecuter
    {
        Task<IExecutionResult> ExecuteAsync(
            QueryRequest request,
            CancellationToken cancellationToken);
    }
}
