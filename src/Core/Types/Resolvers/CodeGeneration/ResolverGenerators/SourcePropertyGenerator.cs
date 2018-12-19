using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SourcePropertyGenerator
       : ResolverSourceCodeGenerator<SourceResolverDescriptor>
    {
        protected override bool IsAsync => false;

        protected override void GenerateResolverInvocation(
            SourceResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{resolverDescriptor.SourceType.GetTypeName()}>();");
            HandleExceptionsSync(source, s =>
            {
                s.Append($"return Task.FromResult<object>(source.{resolverDescriptor.Field.Member.Name});");
            });
        }

        protected override bool CanHandle(SourceResolverDescriptor descriptor)
        {
            return descriptor.IsProperty;
        }
    }
}
