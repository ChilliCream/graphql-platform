using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class ResolverPropertyGenerator
        : ResolverSourceCodeGenerator<ResolverDescriptor>
    {
        protected override bool IsAsync => false;

        protected override void GenerateResolverInvocation(
            ResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Resolver)}<{resolverDescriptor.ResolverType.GetTypeName()}>();");
            HandleExceptionsSync(source, s =>
            {
                s.Append($"return Task.FromResult<object>(resolver.{resolverDescriptor.Field.Member.Name});");
            });
        }

        protected override bool CanHandle(ResolverDescriptor descriptor)
        {
            return descriptor.IsProperty;
        }
    }
}
