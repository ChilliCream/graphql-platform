using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL subscription
    /// </summary>
    public class SubscriptionOperationDescriptor: OperationDescriptor
    {
        public override string Name => NamingConventions.SubscriptionServiceNameFromTypeName(ResultTypeReference.Name);

        public SubscriptionOperationDescriptor(ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<NamedTypeReferenceDescriptor> arguments,
            string bodyString) : base(resultTypeReference, @namespace, arguments, bodyString)
        {
        }
    }
}
