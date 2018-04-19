using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    private sealed class AsyncResolverMethodGenerator
            : SourceCodeGenerator
        {
            protected override void GenerateResolverInvocation(FieldResolverDescriptor resolverDescriptor, StringBuilder source)
            {
                source.AppendLine($"var resolver = ctx.Service<{resolverDescriptor.ResolverType.FullName}>();");
                source.AppendLine($"return await source.{resolverDescriptor.MemberName} (");

                if (resolverDescriptor.Arguments.Any())
                {
                    string arguments = string.Join(", ", resolverDescriptor.Arguments.Select(t => t.Name));
                    source.AppendLine(arguments);
                }

                source.Append(");");
            }

            public override bool CanGenerate(
                FieldResolverDescriptor resolverDescriptor)
                    => resolverDescriptor.IsAsync;
        }
}