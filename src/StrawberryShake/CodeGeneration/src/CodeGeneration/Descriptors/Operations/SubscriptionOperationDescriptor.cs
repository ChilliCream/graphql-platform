using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL subscription
    /// </summary>
    public class SubscriptionOperationDescriptor : OperationDescriptor
    {
        public SubscriptionOperationDescriptor(
            NameString operationName,
            ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<PropertyDescriptor> arguments,
            string bodyString)
            : base(operationName, resultTypeReference, @namespace, arguments, bodyString)
        {
        }

        public override NameString Name =>
            NamingConventions.SubscriptionServiceNameFromTypeName(_operationName);
    }
}
