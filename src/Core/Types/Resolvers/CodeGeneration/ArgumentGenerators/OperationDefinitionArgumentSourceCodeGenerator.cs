namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class OperationDefinitionArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.OperationDefinition;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.Operation)}";
        }
    }
}
