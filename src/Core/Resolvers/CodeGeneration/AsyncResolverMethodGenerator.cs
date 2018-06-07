using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncResolverMethodGenerator
        : SourceCodeGenerator
    {
        protected override void GenerateResolverInvocation(
            FieldResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine("Func<Task<object>> f = async () => {");
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Service)}<{GetTypeName(resolverDescriptor.ResolverType)}>();");

            HandleExceptions(source, s =>
            {
                s.Append($"return await resolver.{resolverDescriptor.Member.Name} (");
                if (resolverDescriptor.ArgumentDescriptors.Any())
                {
                    string arguments = string.Join(", ",
                        resolverDescriptor.ArgumentDescriptors.Select(t => t.Name));
                    s.Append(arguments);
                }
                s.Append(");");
            });

            source.AppendLine("};");
            source.Append("return f();");
        }

        public override bool CanGenerate(
            FieldResolverDescriptor resolverDescriptor)
                => resolverDescriptor.IsAsync
                    && resolverDescriptor.IsMethod
                    && resolverDescriptor.Kind == FieldResolverKind.Collection;
    }
}
