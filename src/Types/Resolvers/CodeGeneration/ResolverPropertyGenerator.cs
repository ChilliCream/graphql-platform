using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ResolverPropertyGenerator
        : SourceCodeGenerator<ResolverDescriptor>
    {
        protected override void GenerateResolverInvocation(
            ResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Service)}<{GetTypeName(resolverDescriptor.ResolverType)}>();");
            HandleExceptions(source, s =>
            {
                s.Append($"return resolver.{resolverDescriptor.Field.Member.Name};");
            });
        }

        public override bool CanGenerate(
            IFieldResolverDescriptor resolverDescriptor)
        {
            return resolverDescriptor is ResolverDescriptor d
                && d.IsProperty;
        }
    }
}
