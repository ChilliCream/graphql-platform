namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class FieldArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.Field;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.Field)}";
        }
    }
}
