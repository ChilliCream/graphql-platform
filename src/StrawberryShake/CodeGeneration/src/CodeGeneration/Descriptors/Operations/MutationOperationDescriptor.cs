using System.Collections.Generic;
using HotChocolate;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL mutation
    /// </summary>
    public class MutationOperationDescriptor : OperationDescriptor
    {
        public MutationOperationDescriptor(
            NameString name,
            string @namespace,
            ITypeDescriptor resultTypeReference,
            IReadOnlyList<PropertyDescriptor> arguments,
            string bodyString)
            : base(
                name,
                new RuntimeTypeInfo(CreateMutationServiceName(name), @namespace),
                resultTypeReference,
                arguments,
                bodyString)
        {
        }
    }
}
