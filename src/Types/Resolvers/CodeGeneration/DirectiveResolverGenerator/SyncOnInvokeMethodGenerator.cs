using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SyncOnInvokeMethodGenerator
        : OnInvokeSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override bool IsAsync => false;

        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor descriptor,
            StringBuilder source)
        {

            HandleExceptions(source, s =>
            {
                s.Append($"ctx.Result = resolver.{descriptor.Method.Name}(");
                GenerateArguments(descriptor, s);
                s.Append(");");
            });

            source.Append("return next.Invoke(ctx);");
        }


        protected override void GenerateArgumentDeclaration(
            DirectiveMiddlewareDescriptor descriptor,
            StringBuilder source)
        {
            // do nothing
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return !descriptor.IsAsync;
        }
    }
}
