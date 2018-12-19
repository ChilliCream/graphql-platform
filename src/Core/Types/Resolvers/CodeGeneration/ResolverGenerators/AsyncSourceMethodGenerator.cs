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
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{resolverDescriptor.SourceType.GetTypeName()}>();");

            HandleExceptions(source, s =>
            {
                s.Append($"return await source.{resolverDescriptor.Field.Member.Name}(");
                if (resolverDescriptor.Arguments.Count > 0)
                {
                    string arguments = string.Join(", ",
                        resolverDescriptor.Arguments
                            .Select(t => t.VariableName));
                    s.Append(arguments);
                }
                s.AppendLine(");");
            });
        }

        protected override bool CanHandle(SourceResolverDescriptor descriptor)
        {
            return descriptor.IsAsync && descriptor.IsMethod;
        }
    }
}
