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
            HandleExceptionsAsync(source, s =>
            {
                s.Append($"return System.Threading.Tasks.Task.FromResult<object>(resolver.{resolverDescriptor.Method.Name}(");
                if (resolverDescriptor.Arguments.Count > 0)
                {
                    string arguments = string.Join(", ",
                        resolverDescriptor.Arguments
                            .Select(t => t.VariableName));
                    s.Append(arguments);
                }
                s.Append("));");
            });
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return descriptor.Kind == MiddlewareKind.OnInvoke
                && !descriptor.IsAsync;
        }
    }
}
