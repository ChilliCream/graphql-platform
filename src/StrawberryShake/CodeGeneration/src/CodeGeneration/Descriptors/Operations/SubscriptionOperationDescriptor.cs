using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.Tools.Configuration;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Descriptors.Operations;

/// <summary>
/// Describes a GraphQL subscription
/// </summary>
public class SubscriptionOperationDescriptor : OperationDescriptor
{
    public SubscriptionOperationDescriptor(
        string name,
        string @namespace,
        ITypeDescriptor resultTypeReference,
        IReadOnlyList<PropertyDescriptor> arguments,
        byte[] body,
        string bodyString,
        string hashAlgorithm,
        string hashValue,
        RequestStrategy strategy)
        : base(
            name,
            new RuntimeTypeInfo(CreateSubscriptionServiceName(name), @namespace),
            resultTypeReference,
            arguments,
            body,
            bodyString,
            hashAlgorithm,
            hashValue,
            false,
            strategy)
    {
    }
}
