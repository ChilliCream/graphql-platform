using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SyncOnInvokeMethodGenerator
        : MiddlewareSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override bool IsAsync => false;

        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor descriptor,
            StringBuilder source)
        {

            HandleExceptions(source, s =>
            {
                if (descriptor.HasResult)
                {
                    s.Append("ctx.Result = ");
                }
                s.Append($"resolver.{descriptor.Method.Name}(");
                GenerateArguments(descriptor, s);
                s.Append(");");
            });

            source.Append("return next.Invoke(ctx);");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return !descriptor.IsAsync;
        }
    }
}
