using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;

namespace Prometheus.Execution
{
    public interface IDocumentExecuter
    {
        Task<QueryResult> ExecuteAsync(
            ISchema schema, string query,
            string operationName, IDictionary<string, object> variableValues,
            object initialValue, CancellationToken cancellationToken);
    }
}