using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public interface IDocumentExecuter
    {
        Task<QueryResult> ExecuteAsync(
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

    public class QueryResult
    {
        public QueryResult(IReadOnlyDictionary<string, object> data)
        {
            Data = data;
        }

        public QueryResult(IReadOnlyCollection<QueryError> errors)
        {
            Errors = errors;
        }

        public QueryResult(IReadOnlyDictionary<string, object> data, IReadOnlyCollection<QueryError> errors)
        {
            Data = data;
            Errors = errors;
        }

        public IReadOnlyDictionary<string, object> Data { get; }
        public IReadOnlyCollection<QueryError> Errors { get; }
    }

    public class QueryError
    {

    }

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

    public class VariableCollection
        : IVariableCollection
    {
        private readonly IDictionary<string, object> _variables;
        public VariableCollection()
            : this(new Dictionary<string, object>())
        {

        }

        public VariableCollection(IDictionary<string, object> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            _variables = variables;
        }

        public T GetVariable<T>(string variableName)
        {
            if (_variables.TryGetValue(variableName, out var value))
            {
                return (T)value;
            }
            return default(T);
        }
    }
}