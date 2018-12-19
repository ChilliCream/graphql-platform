using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public static class QueryExecuterExtensions
    {
        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            QueryRequest request)
        {
            return executer.ExecuteAsync(request, CancellationToken.None);
        }
    }
}
