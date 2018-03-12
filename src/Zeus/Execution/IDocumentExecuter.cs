using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public interface IDocumentExecuter
    {
        Task<QueryResult> ExecuteAsync(
            ISchema schema, string query,
            string operationName, IDictionary<string, object> variableValues,
            object initialValue, CancellationToken cancellationToken);
    }
}