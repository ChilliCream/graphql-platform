using System.Collections.Generic;
using HotChocolate;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration
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
            string hashValue)
            : base(
                name,
                new RuntimeTypeInfo(CreateQueryServiceName(name), @namespace),
                resultTypeReference,
                arguments,
                body,
                bodyString,
                hashAlgorithm,
                hashValue)
        {
        }
    }
}
