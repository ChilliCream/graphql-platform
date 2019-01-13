using System.Text;

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
            if (descriptor.HasResult)
            {
                source.Append("ctx.Result = ");
            }
            source.Append($"resolver.{descriptor.Method.Name}(");
            GenerateArguments(descriptor, source);
            source.Append(");");

            source.Append("return next.Invoke(ctx);");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return !descriptor.IsAsync;
        }
    }
}
