using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class OnAfterInvokeSourceCodeGenerator<T>
        : ResolverSourceCodeGeneratorBase<T>
        where T : IDirectiveMiddlewareDescriptor
    {
        protected override IReadOnlyCollection<ArgumentSourceCodeGenerator> ArgumentGenerators =>
            ArgumentGeneratorCollections.OnAfterInvokeArguments;
        protected override void GenerateDelegateHeader(
            string delegateName, T descriptor, StringBuilder source)
        {
            source.AppendLine($"/* @{descriptor.DirectiveName} */");
            source.Append($"public static {nameof(OnAfterInvokeResolverAsync)}");
            source.Append(" ");
            source.Append(delegateName);
            source.Append(" ");
            source.Append(" = ");
            source.Append("(ctx, dir, res, ct) => {");
            source.AppendLine();
        }

        protected override void GenerateDelegateFooter(
            string delegateName, T descriptor, StringBuilder source)
        {
            source.AppendLine();
            source.Append("};");
        }

        protected override IEnumerable<ArgumentDescriptor> GetArguments(
           T descriptor)
        {
            return descriptor.Arguments;
        }
    }


}
