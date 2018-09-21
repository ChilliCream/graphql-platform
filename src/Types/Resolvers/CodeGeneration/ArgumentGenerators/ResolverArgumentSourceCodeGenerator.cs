using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ResolverArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Resolver;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"new System.Func<System.Threading.Tasks.Task<{descriptor.Type.GetTypeName()}>>(async () => ({descriptor.Type.GetTypeName()})await exec())";
        }
    }
}
