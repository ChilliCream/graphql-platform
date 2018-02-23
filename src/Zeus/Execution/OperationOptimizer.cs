using System;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public class OperationOptimizer
        : IOperationOptimizer
    {
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

            OperationDefinition operation = GetOperation(document, operationName);
            OperationContext operationContext = new OperationContext(schema, document, operation);
            return new OptimizedOperation(operationContext);
        }

        private static OperationDefinition GetOperation(QueryDocument document, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (document.Operations.Count == 1)
                {
                    return document.Operations.Values.First();
                }
                throw new Exception("TODO: Query Exception");
            }
            else
            {
                if (document.Operations.TryGetValue(name, out var operation))
                {
                    return operation;
                }
                throw new Exception("TODO: Query Exception");
            }
        }
    }
}