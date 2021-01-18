using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQL query
    /// </summary>
    public class QueryOperationDescriptor : OperationDescriptor
    {
        public QueryOperationDescriptor(ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<NamedTypeReferenceDescriptor> arguments,
            string bodyString)
            : base(resultTypeReference, @namespace, arguments, bodyString)
        {
        }

        public override string Name =>
            NamingConventions.QueryServiceNameFromTypeName(ResultTypeReference.Name);
    }
}
