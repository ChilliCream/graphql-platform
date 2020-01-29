using System;
using System.Collections.Generic;

namespace StrawberryShake.Generators.Descriptors
{
    public class ClientDescriptor
        : IClientDescriptor
    {
        public ClientDescriptor(
            string name,
            string ns,
            IReadOnlyList<IOperationDescriptor> operations)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
            Operations = operations
                ?? throw new ArgumentNullException(nameof(operations));
        }

        public string Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<IOperationDescriptor> Operations { get; }

        public IEnumerable<ICodeDescriptor> GetChildren() => Operations;
    }
}
