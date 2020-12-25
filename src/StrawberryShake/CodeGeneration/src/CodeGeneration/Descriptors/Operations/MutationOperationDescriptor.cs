using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl mutation
    /// </summary>
    public class MutationOperationDescriptor: OperationDescriptor
    {
        public override string Name => NamingConventions.MutationServiceNameFromTypeName(ResultTypeReference.Name);

        public MutationOperationDescriptor(
            TypeReferenceDescriptor resultTypeReference,
            IReadOnlyDictionary<string, TypeReferenceDescriptor> arguments) : base(resultTypeReference, arguments)
        {
        }
    }
}
