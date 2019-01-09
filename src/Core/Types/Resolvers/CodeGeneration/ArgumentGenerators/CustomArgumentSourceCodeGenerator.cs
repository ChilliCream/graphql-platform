using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class CustomArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Argument;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            string name = WriteEscapeCharacters(descriptor.Name);
            return $"ctx.{nameof(IResolverContext.Argument)}<{descriptor.Type.GetTypeName()}>(\"{name}\")";
        }
    }
}
