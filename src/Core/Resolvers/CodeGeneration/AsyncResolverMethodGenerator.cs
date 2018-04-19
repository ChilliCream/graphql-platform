using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncResolverMethodGenerator
        : SourceCodeGenerator
    {
        protected override void GenerateResolverInvocation(
            FieldResolverDescriptor resolverDescriptor, StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Service)}<{resolverDescriptor.ResolverType.FullName}>();");
            source.AppendLine($"return await resolver.{resolverDescriptor.MemberName} (");

            if (resolverDescriptor.Arguments.Any())
            {
                string arguments = string.Join(", ", 
                    resolverDescriptor.Arguments.Select(t => t.Name));
                source.AppendLine(arguments);
            }

            source.Append(");");
        }

        public override bool CanGenerate(
            FieldResolverDescriptor resolverDescriptor)
                => resolverDescriptor.IsAsync
                    && resolverDescriptor.IsMethod
                    && resolverDescriptor.Kind == FieldResolverKind.Collection;
    }
}