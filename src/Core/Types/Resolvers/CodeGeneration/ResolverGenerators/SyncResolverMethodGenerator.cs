using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class SyncResolverMethodGenerator
        : ResolverSourceCodeGenerator<ResolverDescriptor>
    {
        protected override bool IsAsync => false;

        protected override void GenerateResolverInvocation(
            ResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var resolver = ctx.{nameof(IResolverContext.Resolver)}<{resolverDescriptor.ResolverType.GetTypeName()}>();");
            HandleExceptionsSync(source, s =>
            {
                s.Append($"return Task.FromResult<object>(resolver.{resolverDescriptor.Field.Member.Name}(");
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

        protected override bool CanHandle(ResolverDescriptor descriptor)
        {
            return !descriptor.IsAsync && descriptor.IsMethod;
        }
    }
}
