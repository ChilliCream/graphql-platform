using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncOnInvokeMethodGenerator
        : MiddlewareSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override bool IsAsync => true;

        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor resolverDescriptor,
            StringBuilder source)
        {

            if (resolverDescriptor.HasResult)
            {
                source.Append("ctx.Result = ");
            }
            source.Append($"await resolver.{resolverDescriptor.Method.Name}(");
            GenerateArguments(resolverDescriptor, source);
            source.AppendLine(").ConfigureAwait(false);");

            source.AppendLine("await next.Invoke(ctx).ConfigureAwait(false);");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return descriptor.IsAsync;
        }
    }
}
