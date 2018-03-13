using System;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    public class OperationContext
    {
        public OperationContext(
            ISchema schema,
            IQueryDocument queryDocument,
            OperationDefinition operation)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            QueryDocument = queryDocument ?? throw new ArgumentNullException(nameof(queryDocument));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public ISchema Schema { get; }

        public IQueryDocument QueryDocument { get; }

        public OperationDefinition Operation { get; }
    }
}