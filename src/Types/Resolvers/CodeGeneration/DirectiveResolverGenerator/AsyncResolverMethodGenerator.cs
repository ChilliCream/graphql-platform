using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncDirectiveResolverMethodGenerator
        : DirectiveResolverSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Resolver)}<{resolverDescriptor.Type.GetTypeName()}>();");
            source.AppendLine("Func<Task<object>> f = async () => {");

            HandleExceptions(source, s =>
            {
                s.Append($"return await resolver.{resolverDescriptor.Method.Name}(");
                if (resolverDescriptor.Arguments.Count > 0)
                {
                    string arguments = string.Join(", ",
                        resolverDescriptor.Arguments
                            .Select(t => t.VariableName));
                    s.Append(arguments);
                }
                s.Append(");");
            });

            source.AppendLine("};");
            source.Append("return f();");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return descriptor.IsAsync;
        }
    }
}
