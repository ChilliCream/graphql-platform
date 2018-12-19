using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ResultArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.ResolverResult;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"({descriptor.Type.GetTypeName()})rr";
        }
    }
}
