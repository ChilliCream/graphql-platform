using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ResolverPropertyGenerator
        : SourceCodeGenerator
    {
        protected override void GenerateResolverInvocation(FieldResolverDescriptor resolverDescriptor, StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Service)}<{resolverDescriptor.ResolverType.FullName}>();");
            source.AppendLine($"return Task.FromResult<object>(resolver.{resolverDescriptor.Member.Name});");
        }

        public override bool CanGenerate(
            FieldResolverDescriptor resolverDescriptor)
                => resolverDescriptor.Kind == FieldResolverKind.Collection
                    && !resolverDescriptor.IsMethod;
    }
}