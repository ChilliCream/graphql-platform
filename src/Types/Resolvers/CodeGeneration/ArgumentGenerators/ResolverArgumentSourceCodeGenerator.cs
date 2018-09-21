using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ResolverArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Resolver;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"({descriptor.Type.GetTypeName()})await exec()";
        }
    }
}
