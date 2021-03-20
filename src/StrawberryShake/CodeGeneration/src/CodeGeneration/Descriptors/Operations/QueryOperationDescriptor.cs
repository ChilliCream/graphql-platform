using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Execution.Options;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Descriptors.Operations
{
    /// <summary>
    /// Describes a GraphQL query
    /// </summary>
    public class QueryOperationDescriptor : OperationDescriptor
    {
        public QueryOperationDescriptor(
            NameString name,
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
                new RuntimeTypeInfo(CreateQueryServiceName(name), @namespace),
                resultTypeReference,
                arguments,
                body,
                bodyString,
                hashAlgorithm,
                hashValue,
                strategy)
        {
        }
    }
}
