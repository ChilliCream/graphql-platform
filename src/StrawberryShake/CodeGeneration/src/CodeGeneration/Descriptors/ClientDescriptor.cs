using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL client class, that bundles all operations defined in a single class.
    /// </summary>
    public class ClientDescriptor : ICodeDescriptor
    {
        public ClientDescriptor(
            NameString name,
            string @namespace,
            List<OperationDescriptor> operations)
        {
            Name = name;
            Operations = operations;
            Namespace = @namespace;
        }

        /// <summary>
        /// The name of the client
        /// </summary>
        public NameString Name { get; }

        public string Namespace { get; }

        /// <summary>
        /// The operations that are contained in this client class
        /// </summary>
        public List<OperationDescriptor> Operations { get; }
    }
}
