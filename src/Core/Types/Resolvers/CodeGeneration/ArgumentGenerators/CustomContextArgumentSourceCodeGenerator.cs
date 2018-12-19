using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class CustomContextArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.CustomContext;

        protected override string Generate(
            ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.CustomContext)}<{descriptor.Type.GetTypeName()}>()";
        }
    }
}
