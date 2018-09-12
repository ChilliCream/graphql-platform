using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class DirectiveResolverSourceCodeGenerator<T>
        : ResolverSourceCodeGeneratorBase<T>
        where T : IDirectiveMiddlewareDescriptor
    {
        protected override void GenerateDelegateHeader(
            string delegateName, T descriptor, StringBuilder source)
        {
            source.AppendLine($"/* @{descriptor.DirectiveName} */");
            source.Append($"public static {nameof(DirectiveResolver)}");
            source.Append(" ");
            source.Append(delegateName);
            source.Append(" ");
            source.Append(" = ");
            source.Append("(dctx, ctx, ct) => {");
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
