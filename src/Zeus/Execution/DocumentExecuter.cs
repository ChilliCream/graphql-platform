using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;
using Zeus.Parser;

namespace Zeus.Execution
{
    public class DocumentExecuter
        : IDocumentExecuter
    {
        private readonly object _sync = new object();
        private readonly QueryDocumentReader _queryDocumentReader = new QueryDocumentReader();

        private readonly IOperationOptimizer _operationOptimizer;
        private readonly IOperationExecuter _operationExecuter;

        private ImmutableDictionary<string, QueryDocument> _cache = ImmutableDictionary<string, QueryDocument>.Empty;
        private LinkedList<string> _cachedQueries = new LinkedList<string>();

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
            ISchema schema, string query,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken)
        {
            try
            {
                QueryDocument document = ParseQueryDocument(query);

                VariableCollection variableCollection = variables == null
                    ? new VariableCollection()
                    : new VariableCollection(variables);

                IOptimizedOperation operation = _operationOptimizer
                    .Optimize(schema, document, operationName);

                IReadOnlyDictionary<string, object> operationResult = await _operationExecuter
                    .ExecuteAsync(operation, variableCollection, initialValue, cancellationToken);

                return new QueryResult(operationResult);
            }
            catch (GraphQLQueryException ex)
            {
                return new QueryResult(new QueryError(ex.Message));
            }
        }

        private QueryDocument ParseQueryDocument(string query)
        {
            string normalizedQuery = NormalizeQuery(query);

            if (!_cache.TryGetValue(normalizedQuery, out var queryDocument))
            {
                lock (_sync)
                {
                    queryDocument = _queryDocumentReader.Read(query);

                    _cache = _cache.SetItem(normalizedQuery, queryDocument);
                    _cachedQueries.AddFirst(normalizedQuery);

                    if (_cachedQueries.Count > 100)
                    {
                        _cache = _cache.Remove(_cachedQueries.Last.Value);
                        _cachedQueries.RemoveLast();
                    }
                }
            }

            return queryDocument;
        }

        private string NormalizeQuery(string query)
        {
            return query;
        }
    }
}