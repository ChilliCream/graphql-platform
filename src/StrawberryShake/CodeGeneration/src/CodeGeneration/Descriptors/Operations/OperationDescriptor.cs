using System.Collections.Generic;
using HotChocolate;
using StrawberryShake.CodeGeneration.Properties;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL operation
    /// </summary>
    public abstract class OperationDescriptor : ICodeDescriptor
    {
        public OperationDescriptor(
            NameString name,
            RuntimeTypeInfo runtimeType,
            ITypeDescriptor resultTypeReference,
            IReadOnlyList<PropertyDescriptor> arguments,
            string bodyString)
        {
            Name = name;
            RuntimeType = runtimeType;
            ResultTypeReference = resultTypeReference;
            Arguments = arguments;
            BodyString = bodyString;
        }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public NameString Name { get; }

        public RuntimeTypeInfo RuntimeType { get; }

        /// <summary>
        /// Gets the type the operation returns
        /// </summary>
        public ITypeDescriptor ResultTypeReference { get; }

        public string BodyString { get; }

        /// <summary>
        /// The arguments the operation takes.
        /// </summary>
        public IReadOnlyList<PropertyDescriptor> Arguments { get; }
    }
}
