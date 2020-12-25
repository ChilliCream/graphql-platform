using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl operation
    /// </summary>
    public abstract class OperationDescriptor : ICodeDescriptor
    {
        /// <summary>
        /// The name of the operation
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The type the operation returns
        /// </summary>
        public TypeReferenceDescriptor ResultTypeReference { get; }

        /// <summary>
        /// The arguments the operation takes.
        /// </summary>
        public IReadOnlyDictionary<string, TypeReferenceDescriptor> Arguments { get; }

        public OperationDescriptor(
            TypeReferenceDescriptor resultTypeReference,
            IReadOnlyDictionary<string, TypeReferenceDescriptor> arguments)
        {
            ResultTypeReference = resultTypeReference;
            Arguments = arguments;
        }
    }
}
