using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL mutation
    /// </summary>
    public class MutationOperationDescriptor : OperationDescriptor
    {
        public MutationOperationDescriptor(
            NameString operationName,
            ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<PropertyDescriptor> arguments,
            string bodyString)
            : base(operationName, resultTypeReference, @namespace, arguments, bodyString)
        {
        }

        public override NameString Name =>
            NamingConventions.CreateMutationServiceName(OperationName);
    }
}
