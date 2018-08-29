using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncSourceMethodGenerator
        : SourceCodeGenerator<MemberResolverDescriptor>
    {
        protected override void GenerateResolverInvocation(
            MemberResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            source.AppendLine($"var source = ctx.{nameof(IResolverContext.Parent)}<{GetTypeName(resolverDescriptor.SourceType)}>();");
            source.AppendLine("Func<Task<object>> f = async () => {");

            HandleExceptions(source, s =>
            {
                s.Append($"return await source.{resolverDescriptor.Field.Member.Name}(");
                if (resolverDescriptor.Arguments.Any())
                {
                    string arguments = string.Join(", ",
                        resolverDescriptor.Arguments
                            .Select(t => t.VariableName));
                    s.Append(arguments);
                }
                s.AppendLine(");");
            });

            source.AppendLine("};");
            source.Append("return f();");
        }

        public override bool CanGenerate(
            IFieldResolverDescriptor resolverDescriptor)
        {
            return resolverDescriptor is MemberResolverDescriptor d
                && d.IsAsync && d.IsMethod;
        }
    }
}
