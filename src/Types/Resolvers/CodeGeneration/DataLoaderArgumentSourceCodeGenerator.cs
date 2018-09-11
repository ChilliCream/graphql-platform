using HotChocolate.Internal;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DataLoaderArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.DataLoader;

        protected override string Generate(
            string delegateName,
            ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.DataLoader)}<{descriptor.Type.GetTypeName()}>()";

        }
    }
}
