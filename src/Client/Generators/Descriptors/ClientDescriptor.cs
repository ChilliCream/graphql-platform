using System;
using System.Collections.Generic;

namespace StrawberryShake.Generators.Descriptors
{
    public class ClientDescriptor
        : IClientDescriptor
    {
        public ClientDescriptor(
            string name,
            IReadOnlyList<IOperationDescriptor> operations)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Operations = operations
                ?? throw new ArgumentNullException(nameof(operations));
        }

        public string Name { get; }

        public IReadOnlyList<IOperationDescriptor> Operations { get; }

        public IEnumerable<ICodeDescriptor> GetChildren() => Operations;
    }
}
