using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SyncSourceMethodGenerator
        : SourceCodeGenerator
    {
        protected override void GenerateResolverInvocation(
            FieldResolverDescriptor resolverDescriptor, StringBuilder source)
        {
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{resolverDescriptor.ResolverType.FullName}>();");
            source.AppendLine($"return source.{resolverDescriptor.Member.Name} (");

            if (resolverDescriptor.ArgumentDescriptors.Any())
            {
                string arguments = string.Join(", ",
                    resolverDescriptor.ArgumentDescriptors.Select(t => t.Name));
                source.AppendLine(arguments);
            }

            source.Append(");");
        }

        public override bool CanGenerate(
            FieldResolverDescriptor resolverDescriptor)
                => !resolverDescriptor.IsAsync
                    && resolverDescriptor.IsMethod
                    && resolverDescriptor.Kind == FieldResolverKind.Source;
    }
}
