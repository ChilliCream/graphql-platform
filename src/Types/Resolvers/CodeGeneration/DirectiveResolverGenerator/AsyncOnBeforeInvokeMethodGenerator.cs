using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncOnBeforeInvokeMethodGenerator
        : OnBeforeInvokeSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Resolver)}<{resolverDescriptor.Type.GetTypeName()}>();");
            source.AppendLine("Func<Task> f = async () => {");

            source.Append($"await resolver.{resolverDescriptor.Method.Name}(");
            GenerateArguments(resolverDescriptor, source);
            source.Append(");");

            source.AppendLine("};");
            source.Append("return f();");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return descriptor.Kind == MiddlewareKind.OnBeforeInvoke
                && descriptor.IsAsync;
        }
    }
}
