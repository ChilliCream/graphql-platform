using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class SubscriptionOperationDescriptor: OperationDescriptor
    {
        public override string Name => NamingConventions.SubscriptionServiceNameFromTypeName(ResultType.Name);

        public SubscriptionOperationDescriptor(
            TypeDescriptor resultType,
            IReadOnlyDictionary<string, TypeDescriptor> arguments) : base(resultType, arguments)
        {
        }
    }
}
