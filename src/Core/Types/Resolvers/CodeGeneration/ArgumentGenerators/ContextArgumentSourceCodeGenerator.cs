namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class ContextArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Context;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return "ctx";
        }
    }
}
