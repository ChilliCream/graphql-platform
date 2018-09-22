using System;
using System.Linq;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ResolverArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Resolver;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            Type expectedValueType = descriptor.Type
                .GetGenericArguments().Single()
                .GetGenericArguments().Single();

            return $"new System.Func<System.Threading.Tasks.Task<{expectedValueType.GetTypeName()}>>(async () => ({expectedValueType.GetTypeName()})await exec())";
        }
    }
}
