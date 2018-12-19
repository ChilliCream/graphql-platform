namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class FieldSelectionArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.FieldSelection;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.FieldSelection)}";
        }
    }
}
