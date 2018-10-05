using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncOnInvokeMethodGenerator
        : OnInvokeSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override bool IsAsync => true;

        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor descriptor,
            StringBuilder source)
        {
            HandleExceptions(source, s =>
            {
                s.Append($"ctx.Result = await resolver.{descriptor.Method.Name}(");
                GenerateArguments(descriptor, s);
                s.AppendLine(");");
            });

            source.AppendLine("await next.Invoke(ctx)");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return descriptor.IsAsync;
        }
    }
}
