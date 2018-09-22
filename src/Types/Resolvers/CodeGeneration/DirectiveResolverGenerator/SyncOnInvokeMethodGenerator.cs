using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SyncOnInvokeMethodGenerator
        : OnInvokeSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Resolver)}<{resolverDescriptor.Type.GetTypeName()}>();");

            if (resolverDescriptor.Arguments.Any(t => t.Kind == ArgumentKind.ResolverResult))
            {
                source.AppendLine("Func<Task<object>> f = async () => {");

                base.GenerateArgumentDeclaration(resolverDescriptor, source);

                HandleExceptions(source, s =>
                {
                    s.Append($"return resolver.{resolverDescriptor.Method.Name}(");
                    GenerateArguments(resolverDescriptor, s);
                    s.Append(");");
                });

                source.AppendLine("};");
                source.Append("return f();");
            }
            else
            {
                base.GenerateArgumentDeclaration(resolverDescriptor, source);

                HandleExceptionsAsync(source, s =>
                {
                    s.Append($"return System.Threading.Tasks.Task.FromResult<object>(resolver.{resolverDescriptor.Method.Name}(");
                    GenerateArguments(resolverDescriptor, s);
                    s.Append("));");
                });
            }
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
            return descriptor.Kind == MiddlewareKind.OnInvoke
                && !descriptor.IsAsync;
        }
    }
}
