namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SchemaArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Schema;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.Schema)}";
        }
    }
}
