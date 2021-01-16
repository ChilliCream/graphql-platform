using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl mutation
    /// </summary>
    public class MutationOperationDescriptor: OperationDescriptor
    {
        public override string Name => NamingConventions.MutationServiceNameFromTypeName(ResultTypeReference.Name);

        public MutationOperationDescriptor(ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<NamedTypeReferenceDescriptor> arguments,
            string bodyString) : base(resultTypeReference, @namespace, arguments, bodyString)
        {
        }
    }
}
