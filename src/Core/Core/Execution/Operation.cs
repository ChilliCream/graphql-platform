using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class Operation
        : IOperation
    {
        public Operation(
            DocumentNode query, OperationDefinitionNode definition,
            ObjectType rootType, object rootValue)
        {
            Query = query
                ?? throw new ArgumentNullException(nameof(query));
            Definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
            RootType = rootType
                ?? throw new ArgumentNullException(nameof(rootType));
            RootValue = rootValue;
        }

        public DocumentNode Query { get; }

        public OperationDefinitionNode Definition { get; }

        public ObjectType RootType { get; }

        public object RootValue { get; }

        public string Name => Definition.Name?.Value;

        public OperationType Type => Definition.Operation;
    }
}
