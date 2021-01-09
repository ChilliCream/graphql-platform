using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl mutation
    /// </summary>
    public class MutationOperationDescriptor: OperationDescriptor
    {
        public override string Name => NamingConventions.MutationServiceNameFromTypeName(ResultTypeReference.Type.Name);

        public MutationOperationDescriptor(
            TypeReferenceDescriptor resultTypeReference,
            IReadOnlyDictionary<string, TypeReferenceDescriptor> arguments) : base(resultTypeReference, arguments)
        {
        }
    }
}
