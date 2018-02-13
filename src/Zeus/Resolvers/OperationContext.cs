using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public class OperationContext
    {
        public OperationContext(
            ISchema schema,
            QueryDocument queryDocument,
            OperationDefinition operation)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            QueryDocument = queryDocument ?? throw new ArgumentNullException(nameof(queryDocument));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public ISchema Schema { get; }

        public QueryDocument QueryDocument { get; }

        public OperationDefinition Operation { get; }
    }
}