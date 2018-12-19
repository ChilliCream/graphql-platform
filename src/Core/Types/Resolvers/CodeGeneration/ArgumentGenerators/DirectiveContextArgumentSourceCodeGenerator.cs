namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DirectiveContextArgumentSourceCodeGenerator
        : ContextArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.DirectiveContext;
    }
}
