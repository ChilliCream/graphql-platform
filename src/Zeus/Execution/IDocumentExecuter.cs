using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public interface IDocumentExecuter
    {
        Task<IDictionary<string, object>> ExecuteAsync(
            ISchema schema, QueryDocument document,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken);
    }

    public interface IDocumentValidator
    {
        DocumentValidationReport Validate(ISchema schema, QueryDocument document);
    }

    public class DocumentValidationReport
    {
        
    }
}