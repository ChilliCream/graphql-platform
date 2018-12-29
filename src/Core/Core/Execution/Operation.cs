using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class Operation
        : IOperation
    {
        public Operation(
            DocumentNode query, OperationDefinitionNode node,
            ObjectType rootType, object rootValue)
        {
            Query = query
                ?? throw new ArgumentNullException(nameof(query));
            Node = node
                ?? throw new ArgumentNullException(nameof(node));
            RootType = rootType
                ?? throw new ArgumentNullException(nameof(rootType));
            RootValue = rootValue
                ?? throw new ArgumentNullException(nameof(rootValue));
        }

        public object RootValue { get; }

        public ObjectType RootType { get; }

        public DocumentNode Query { get; }

        public OperationDefinitionNode Node { get; }
    }
}
