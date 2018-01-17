using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Execution
{
    public interface IDocumentExecuter
    {
        Task<IDictionary<string, object>> ExecuteAsync(
            ISchema schema, IDocument document,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken);
    }
}