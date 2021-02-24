using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL query
    /// </summary>
    public class QueryOperationDescriptor : OperationDescriptor
    {
        public QueryOperationDescriptor(
            NameString operationName,
            ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<PropertyDescriptor> arguments,
            string bodyString)
            : base(operationName, resultTypeReference, @namespace, arguments, bodyString)
        {
        }

        public override NameString Name =>
            NamingConventions.CreateQueryServiceName(OperationName);
    }
}
