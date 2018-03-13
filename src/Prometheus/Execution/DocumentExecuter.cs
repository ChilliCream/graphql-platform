using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;
using Prometheus.Parser;

namespace Prometheus.Execution
{
    public class DocumentExecuter
        : IDocumentExecuter
    {
        private readonly QueryDocumentReader _queryDocumentReader = new QueryDocumentReader();
        private readonly ICache<string, IQueryDocument> _cache = new Cache<string, IQueryDocument>();

        private readonly IOperationOptimizer _operationOptimizer;
        private readonly IOperationExecuter _operationExecuter;

        public DocumentExecuter()
            : this(DefaultServiceProvider.Instance)
        {
        }

        public DocumentExecuter(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _operationOptimizer = new OperationOptimizer();
            _operationExecuter = new OperationExecuter(serviceProvider);
        }

        public async Task<QueryResult> ExecuteAsync(
            ISchema schema, string query, string operationName,
            IDictionary<string, object> variableValues,
            object initialValue, CancellationToken cancellationToken)
        {
            try
            {
                IQueryDocument document = ParseQueryDocument(query);

                IOptimizedOperation operation = _operationOptimizer
                    .Optimize(schema, document, operationName);

                VariableCollection variableCollection = new VariableCollection(
                    schema, operation.Operation, variableValues);

                IReadOnlyDictionary<string, object> operationResult = await _operationExecuter
                    .ExecuteAsync(operation, variableCollection, initialValue, cancellationToken);

                return new QueryResult(operationResult);
            }
            catch (GraphQLQueryException ex)
            {
                return new QueryResult(ex.Messages.Select(t => new QueryError(t)).ToArray());
            }
        }

        private IQueryDocument ParseQueryDocument(string query)
        {
            string normalizedQuery = NormalizeQuery(query);
            return _cache.GetOrCreate(normalizedQuery,
                () => _queryDocumentReader.Read(query));
        }

        private string NormalizeQuery(string query)
        {
            return query;
        }
    }
}