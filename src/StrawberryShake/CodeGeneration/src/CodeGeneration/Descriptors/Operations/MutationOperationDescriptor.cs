using System.Collections.Generic;
using HotChocolate;
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
        RequestStrategy strategy,
        bool hasUpload)
        : base(
            name,
            new RuntimeTypeInfo(CreateMutationServiceName(name), @namespace),
            resultTypeReference,
            arguments,
            body,
            bodyString,
            hashAlgorithm,
            hashValue,
            strategy,
            hasUpload)
    {
    }
}
