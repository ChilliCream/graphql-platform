using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL mutation
    /// </summary>
    public class MutationOperationDescriptor : OperationDescriptor
    {
        public MutationOperationDescriptor(ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<PropertyDescriptor> arguments,
            string bodyString)
            : base(resultTypeReference, @namespace, arguments, bodyString)
        {
        }

        public override NameString Name =>
            NamingConventions.MutationServiceNameFromTypeName(ResultTypeReference.Name);
    }
}
