using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a GraphQl query
    /// </summary>
    public class QueryOperationDescriptor : OperationDescriptor
    {
        public override string Name => NamingConventions.QueryServiceNameFromTypeName(ResultTypeReference.Type.Name);

        public QueryOperationDescriptor(
            TypeReferenceDescriptor resultTypeReference,
            IReadOnlyDictionary<string, TypeReferenceDescriptor> arguments) : base(resultTypeReference, arguments)
        {
        }
    }
}
