using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL operation
    /// </summary>
    public abstract class OperationDescriptor : ICodeDescriptor
    {
        public OperationDescriptor(
            ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<PropertyDescriptor> arguments,
            string bodyString)
        {
            ResultTypeReference = resultTypeReference;
            Arguments = arguments;
            BodyString = bodyString;
            Namespace = @namespace;
        }

        /// <summary>
        /// The name of the operation
        /// </summary>
        public abstract NameString Name { get; }

        public string Namespace { get; }

        /// <summary>
        /// The type the operation returns
        /// </summary>
        public ITypeDescriptor ResultTypeReference { get; }

        public string BodyString { get; }

        /// <summary>
        /// The arguments the operation takes.
        /// </summary>
        public IReadOnlyList<PropertyDescriptor> Arguments { get; }
    }
}
