using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SourcePropertyGenerator
       : SourceCodeGenerator
    {
        protected override void GenerateResolverInvocation(FieldResolverDescriptor resolverDescriptor, StringBuilder source)
        {
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{resolverDescriptor.ResolverType.FullName}>();");
            source.AppendLine($"return Task.FromResult<object>(source.{resolverDescriptor.Member.Name});");
        }

        public override bool CanGenerate(
            FieldResolverDescriptor resolverDescriptor)
                => resolverDescriptor.Kind == FieldResolverKind.Source
                    && !resolverDescriptor.IsMethod;
    }
}