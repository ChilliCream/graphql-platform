using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class QueryOperationDescriptor : OperationDescriptor
    {
        public override string Name => NamingConventions.QueryServiceNameFromTypeName(ResultType.Name);

        public QueryOperationDescriptor(
            TypeDescriptor resultType,
            IReadOnlyDictionary<string, TypeDescriptor> arguments) : base(resultType, arguments)
        {
        }
    }
}
