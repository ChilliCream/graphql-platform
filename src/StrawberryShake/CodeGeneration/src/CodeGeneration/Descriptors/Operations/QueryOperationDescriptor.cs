using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.Tools.Configuration;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Descriptors.Operations;

/// <summary>
/// Describes a GraphQL query
/// </summary>
public class QueryOperationDescriptor : OperationDescriptor
{
    public QueryOperationDescriptor(
        string name,
        string @namespace,
        ITypeDescriptor resultTypeReference,
        IReadOnlyList<PropertyDescriptor> arguments,
        byte[] body,
        string bodyString,
        string hashAlgorithm,
        string hashValue,
        bool hasUpload,
        RequestStrategy strategy)
        : base(
            name,
            new RuntimeTypeInfo(CreateQueryServiceName(name), @namespace),
            resultTypeReference,
            arguments,
            body,
            bodyString,
            hashAlgorithm,
            hashValue,
            hasUpload,
            strategy)
    {
    }
}
