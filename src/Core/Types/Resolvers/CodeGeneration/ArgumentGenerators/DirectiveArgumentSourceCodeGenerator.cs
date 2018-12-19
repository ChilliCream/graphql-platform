namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DirectiveArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Directive;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return "dir";
        }
    }
}
