using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncSourceMethodGenerator
        : ResolverSourceCodeGenerator<SourceResolverDescriptor>
    {
        protected override bool IsAsync => true;

        protected override void GenerateResolverInvocation(
            SourceResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.Append($"var source = ctx.");
            source.Append($"{nameof(IResolverContext.Parent)}<");
            source.Append(resolverDescriptor.SourceType.GetTypeName());
            source.AppendLine(">();");

            source.Append("var resolverTask = source.");
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

        protected override bool CanHandle(SourceResolverDescriptor descriptor)
        {
            return descriptor.IsAsync && descriptor.IsMethod;
        }
    }
}
