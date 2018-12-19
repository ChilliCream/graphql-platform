namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class QueryDocumentArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.QueryDocument;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.QueryDocument)}";
        }
    }
}
