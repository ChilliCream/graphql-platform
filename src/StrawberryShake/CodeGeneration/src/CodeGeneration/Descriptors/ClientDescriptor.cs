using System.Collections.Generic;
using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Properties;

namespace StrawberryShake.CodeGeneration.Descriptors
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
            RuntimeType = new(name, @namespace);
            Operations = operations;
            Documentation =
                string.Format(CodeGenerationResources.ClientDescriptor_Description, Name);
        }

        /// <summary>
        /// Gets the client name
        /// </summary>
        /// <value></value>
        public NameString Name => RuntimeType.Name;

        /// <summary>
        /// The name of the client
        /// </summary>
        public RuntimeTypeInfo RuntimeType { get; }

        /// <summary>
        /// The operations that are contained in this client class
        /// </summary>
        public List<OperationDescriptor> Operations { get; }

        /// <summary>
        /// The documentation for this client
        /// </summary>
        public string Documentation { get; }
    }
}
