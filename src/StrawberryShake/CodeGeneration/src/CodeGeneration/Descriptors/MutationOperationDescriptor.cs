using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class MutationOperationDescriptor: OperationDescriptor
    {
        public override string Name => NamingConventions.MutationServiceNameFromTypeName(ResultType.Name);

        public MutationOperationDescriptor(
            TypeDescriptor resultType,
            IReadOnlyDictionary<string, TypeDescriptor> arguments) : base(resultType, arguments)
        {
        }
    }
}
