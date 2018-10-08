using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class MiddlewareSourceCodeGenerator<T>
        : ResolverSourceCodeGeneratorBase<T>
        where T : IDirectiveMiddlewareDescriptor
    {
        protected override IReadOnlyCollection<ArgumentSourceCodeGenerator> ArgumentGenerators =>
            ArgumentGeneratorCollections.MiddlewareArguments;

        protected abstract bool IsAsync { get; }

        protected override void GenerateDelegateHeader(
            string delegateName, T descriptor, StringBuilder source)
        {
            source.AppendLine($"/* @{descriptor.DirectiveName} */");
            source.Append($"public static {nameof(Middleware)}");
            source.Append(" ");
            source.Append(delegateName);
            source.Append(" ");
            source.Append(" = ");
            if (IsAsync)
            {
                source.Append("next => async ctx => {");
            }
            else
            {
                source.Append("next => ctx => {");
            }
            source.AppendLine();

            source.AppendLine($"var resolver = ctx.{nameof(IDirectiveContext.Resolver)}<{descriptor.Type.GetTypeName()}>();");
            source.AppendLine($"var dir = ctx.{nameof(IDirectiveContext.Directive)};");
            source.AppendLine($"var ct = ctx.{nameof(IDirectiveContext.CancellationToken)};");
            source.AppendLine($"var rr = ctx.{nameof(IDirectiveContext.Result)};");
            source.AppendLine($"if(rr is {typeof(IResolverResult).GetTypeName()} trr)");
            source.AppendLine("{");
            source.AppendLine("rr = trr.Value;");
            source.AppendLine("}");
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

        protected override void HandleExceptions(StringBuilder source, Action<StringBuilder> code)
        {
            source.AppendLine("try");
            source.AppendLine("{");
            code(source);
            source.AppendLine();
            source.AppendLine("}");
            source.AppendLine($"catch(HotChocolate.Execution.QueryException ex)");
            source.AppendLine("{");
            source.AppendLine($"ctx.Result = ex.Errors;");
            source.AppendLine("}");
        }
    }
}
