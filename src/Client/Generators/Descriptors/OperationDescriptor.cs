using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class OperationDescriptor
        : IOperationDescriptor
    {
        public OperationDescriptor(
            string name,
            ObjectType operationType,
            OperationDefinitionNode operation,
            IReadOnlyList<IArgumentDescriptor> arguments,
            IQueryDescriptor query)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            OperationType = operationType
                ?? throw new ArgumentNullException(nameof(operationType));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
            Query = query
                ?? throw new ArgumentNullException(nameof(query));
        }

        public string Name { get; }

        public ObjectType OperationType { get; }

        public OperationDefinitionNode Operation { get; }

        public IReadOnlyList<IArgumentDescriptor> Arguments { get; }

        public IQueryDescriptor Query { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield return Query;

            foreach (IArgumentDescriptor argument in Arguments)
            {
                yield return argument.InputObjectType;
            }
        }
    }
}
