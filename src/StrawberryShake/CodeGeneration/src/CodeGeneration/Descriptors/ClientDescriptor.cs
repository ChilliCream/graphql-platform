using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl client class, that bundles all operations defined in a single class.
    /// </summary>
    public class ClientDescriptor : ICodeDescriptor
    {
        /// <summary>
        /// The name of the client
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The operations that are contained in this client class
        /// </summary>
        public List<OperationDescriptor> Operations { get; }

        public ClientDescriptor(string name, List<OperationDescriptor> operations)
        {
            Name = name;
            Operations = operations;
        }
    }
}
