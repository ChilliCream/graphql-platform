using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SourceArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Source;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.Parent)}<{descriptor.Type.GetTypeName()}>()";
        }
    }
}
