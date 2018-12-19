using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DirectiveArgumentArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.DirectiveArgument;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"dir.GetArgument<{descriptor.Type.GetTypeName()}>(\"{descriptor.Name}\")";
        }
    }
}
