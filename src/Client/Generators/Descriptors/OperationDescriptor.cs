using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.Generators.Descriptors
{
    public class OperationDescriptor
        : IOperationDescriptor
    {
        public OperationDescriptor(
            string name,
            OperationDefinitionNode operation,
            IReadOnlyList<IArgumentDescriptor> arguments)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
        }

        public string Name { get; }

        public OperationDefinitionNode Operation { get; }

        public IReadOnlyList<IArgumentDescriptor> Arguments { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            return Arguments;
        }
    }
}
