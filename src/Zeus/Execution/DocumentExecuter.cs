using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public class DocumentExecuter
        : IDocumentExecuter
    {
        private readonly IOperationOptimizer _operationOptimizer;
        private readonly IOperationExecuter _operationExecuter;

        public DocumentExecuter()
            : this(DefaultServiceProvider.Instance)
        {
        }

        public DocumentExecuter(IServiceProvider serviceProvider)
        {
            _operationOptimizer = new OperationOptimizer();
            _operationExecuter = new OperationExecuter(serviceProvider);
        }

        public async Task<QueryResult> ExecuteAsync(
            ISchema schema, QueryDocument document,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken)
        {
            VariableCollection variableCollection = variables == null
                ? new VariableCollection()
                : new VariableCollection(variables);

            IOptimizedOperation operation = _operationOptimizer
                .Optimize(schema, document, operationName);

            IReadOnlyDictionary<string, object> operationResult = await _operationExecuter
                .ExecuteAsync(operation, variableCollection, initialValue, cancellationToken);

            return new QueryResult(operationResult);
        }
    }
}