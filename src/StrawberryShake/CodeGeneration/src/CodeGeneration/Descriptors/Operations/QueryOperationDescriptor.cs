using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl query
    /// </summary>
    public class QueryOperationDescriptor : OperationDescriptor
    {
        public override string Name => NamingConventions.QueryServiceNameFromTypeName(ResultTypeReference.Name);

        public QueryOperationDescriptor(
            ITypeDescriptor resultTypeReference,
            string @namespace,
            IReadOnlyList<NamedTypeReferenceDescriptor> arguments) : base(resultTypeReference, @namespace, arguments)
        {
        }
    }
}
