using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncSourceMethodGenerator
        : SourceCodeGenerator
    {
        protected override void GenerateResolverInvocation(
            FieldResolverDescriptor resolverDescriptor, StringBuilder source)
        {
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{GetTypeName(resolverDescriptor.ResolverType)}>();");
            source.AppendLine("Func<Task<object>> f = async () => {");

            HandleExceptions(source, s =>
            {
                s.Append($"return await source.{resolverDescriptor.Member.Name}(");
                if (resolverDescriptor.ArgumentDescriptors.Any())
                {
                    string arguments = string.Join(", ",
                        resolverDescriptor.ArgumentDescriptors.Select(t => t.VariableName));
                    s.Append(arguments);
                }
                s.AppendLine(");");
            });

            source.AppendLine("};");
            source.Append("return f();");
        }

        public override bool CanGenerate(
            FieldResolverDescriptor resolverDescriptor)
                => resolverDescriptor.IsAsync
                    && resolverDescriptor.IsMethod
                    && resolverDescriptor.Kind == FieldResolverKind.Source;
    }
}
