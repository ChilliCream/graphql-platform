using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl subscription
    /// </summary>
    public class SubscriptionOperationDescriptor: OperationDescriptor
    {
        public override string Name => NamingConventions.SubscriptionServiceNameFromTypeName(ResultTypeReference.Type.Name);

        public SubscriptionOperationDescriptor(
            TypeReferenceDescriptor resultTypeReference,
            IReadOnlyDictionary<string, TypeReferenceDescriptor> arguments) : base(resultTypeReference, arguments)
        {
        }
    }
}
