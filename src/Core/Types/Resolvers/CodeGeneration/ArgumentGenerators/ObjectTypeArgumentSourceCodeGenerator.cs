namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ObjectTypeArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.ObjectType;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.ObjectType)}";
        }
    }
}
