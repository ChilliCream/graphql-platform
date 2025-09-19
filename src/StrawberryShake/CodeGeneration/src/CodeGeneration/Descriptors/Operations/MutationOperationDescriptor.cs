using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.Tools.Configuration;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Descriptors.Operations;

/// <summary>
/// Describes a GraphQL mutation
/// </summary>
public class MutationOperationDescriptor : OperationDescriptor
{
    public MutationOperationDescriptor(
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
            new RuntimeTypeInfo(CreateMutationServiceName(name), @namespace),
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
