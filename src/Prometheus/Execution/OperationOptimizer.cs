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
        private ICache<IQueryDocument, IOptimizedOperation> _cache =
            new Cache<IQueryDocument, IOptimizedOperation>();

        public IOptimizedOperation Optimize(ISchema schema, 
            IQueryDocument queryDocument, string operationName)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            return _cache.GetOrCreate(queryDocument, () =>
            {
                OperationDefinition operation = queryDocument.GetOperation(operationName);
                OperationContext operationContext = new OperationContext(
                    schema, queryDocument, operation);
                return new OptimizedOperation(operationContext);
            });
        }
    }
}