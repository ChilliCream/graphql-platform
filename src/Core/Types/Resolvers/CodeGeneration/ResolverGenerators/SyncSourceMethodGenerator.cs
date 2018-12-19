using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SyncSourceMethodGenerator
        : ResolverSourceCodeGenerator<SourceResolverDescriptor>
    {
        protected override bool IsAsync => false;

        protected override void GenerateResolverInvocation(
            SourceResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{resolverDescriptor.SourceType.GetTypeName()}>();");
            HandleExceptionsSync(source, s =>
            {
                s.Append($"return Task.FromResult<object>(source.{resolverDescriptor.Field.Member.Name}(");
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

        protected override bool CanHandle(SourceResolverDescriptor descriptor)
        {
            return !descriptor.IsAsync && descriptor.IsMethod;
        }
    }
}
