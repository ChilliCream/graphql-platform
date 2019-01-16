using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncOnInvokeMethodGenerator
        : MiddlewareSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override bool IsAsync => true;

        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor descriptor,
            StringBuilder source)
        {

            if (descriptor.HasResult)
            {
                source.Append("ctx.Result = ");
            }
            source.Append($"await resolver.{descriptor.Method.Name}(");
            GenerateArguments(descriptor, source);
            source.AppendLine(");");

            source.AppendLine("await next.Invoke(ctx);");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return descriptor.IsAsync;
        }
    }
}
