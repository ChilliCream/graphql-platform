using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class OnInvokeResultArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.ResolverResult;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"await exec() as {descriptor.Type.GetTypeName()}";
        }
    }
}
