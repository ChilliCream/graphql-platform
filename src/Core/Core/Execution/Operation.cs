using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class Operation
        : IOperation
    {
        public Operation(
            DocumentNode query,
            OperationDefinitionNode definition,
            IVariableValueCollection variables,
            ObjectType rootType,
            object rootValue)
        {
            Document = query
                ?? throw new ArgumentNullException(nameof(query));
            Definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
            Variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
            RootType = rootType
                ?? throw new ArgumentNullException(nameof(rootType));
            RootValue = rootValue;
        }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Definition { get; }

        public ObjectType RootType { get; }

        public object RootValue { get; }

        public string Name => Definition.Name?.Value;

        public OperationType Type => Definition.Operation;

        public IVariableValueCollection Variables { get; }
    }
}
