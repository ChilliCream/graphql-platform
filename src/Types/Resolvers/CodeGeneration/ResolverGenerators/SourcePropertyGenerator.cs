using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SourcePropertyGenerator
       : ResolverSourceCodeGenerator<SourceResolverDescriptor>
    {
        protected override void GenerateResolverInvocation(
            SourceResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{GetTypeName(resolverDescriptor.SourceType)}>();");
            HandleExceptions(source, s =>
            {
                s.Append($"return source.{resolverDescriptor.Field.Member.Name};");
            });
        }

        protected override bool CanHandle(SourceResolverDescriptor descriptor)
        {
            return descriptor.IsProperty;
        }
    }
}
