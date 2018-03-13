using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Prometheus.Abstractions;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    public class OperationOptimizer
        : IOperationOptimizer
    {
        private readonly object _sync = new object();
        private readonly LinkedList<QueryDocument> _cachedQueries = new LinkedList<QueryDocument>();

        private ImmutableDictionary<QueryDocument, IOptimizedOperation> _cache =
            ImmutableDictionary<QueryDocument, IOptimizedOperation>.Empty;

        public IOptimizedOperation Optimize(ISchema schema, QueryDocument document, string operationName)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (!_cache.TryGetValue(document, out var optimizedOperation))
            {
                lock (_sync)
                {
                    OperationDefinition operation = GetOperation(document, operationName);
                    OperationContext operationContext = new OperationContext(schema, document, operation);
                    optimizedOperation = new OptimizedOperation(operationContext);

                    _cache = _cache.SetItem(document, optimizedOperation);
                    _cachedQueries.AddFirst(document);

                    if (_cachedQueries.Count > 100)
                    {
                        _cache = _cache.Remove(_cachedQueries.Last.Value);
                        _cachedQueries.RemoveLast();
                    }
                }
            }
            return optimizedOperation;
        }

        private static OperationDefinition GetOperation(QueryDocument document, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (document.Operations.Count == 1)
                {
                    return document.Operations.Values.First();
                }

                throw new GraphQLQueryException(
                    "The specified query contains more than one operation. "
                    + "Specify which operation shall be executed.");
            }
            else
            {
                if (document.Operations.TryGetValue(name, out var operation))
                {
                    return operation;
                }

                throw new GraphQLQueryException(
                    $"The specified operation ({name}) could not be "
                    + "found in the specified query.");
            }
        }
    }
}