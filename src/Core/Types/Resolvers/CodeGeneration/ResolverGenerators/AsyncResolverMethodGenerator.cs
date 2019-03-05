using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncResolverMethodGenerator
        : ResolverSourceCodeGenerator<ResolverDescriptor>
    {
        protected override bool IsAsync => true;

        protected override void GenerateResolverInvocation(
            ResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.Append($"var resolver = ctx.");
            source.Append($"{nameof(IResolverContext.Resolver)}<");
            source.Append(resolverDescriptor.ResolverType.GetTypeName());
            source.AppendLine(">();");

            source.Append("var resolverTask = resolver.");
            source.Append($"{resolverDescriptor.Field.Member.Name}(");
            if (resolverDescriptor.Arguments.Count > 0)
            {
                string arguments = string.Join(", ",
                    resolverDescriptor.Arguments
                        .Select(t => t.VariableName));
                source.Append(arguments);
            }
            source.AppendLine(");");

            source.AppendLine("if(resolverTask == null) {");
            source.AppendLine("return null;");
            source.AppendLine("}");
            source.AppendLine("else");
            source.AppendLine("{");
            source.AppendLine("return await resolverTask." +
                "ConfigureAwait(false);");
            source.AppendLine("}");
        }

        protected override bool CanHandle(ResolverDescriptor descriptor)
        {
            return descriptor.IsAsync && descriptor.IsMethod;
        }
    }
}
