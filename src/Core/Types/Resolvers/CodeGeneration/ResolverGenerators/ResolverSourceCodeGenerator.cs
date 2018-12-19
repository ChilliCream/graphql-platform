using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class ResolverSourceCodeGenerator<T>
        : ResolverSourceCodeGeneratorBase<T>
        where T : IFieldResolverDescriptor
    {
        protected override IReadOnlyCollection<ArgumentSourceCodeGenerator> ArgumentGenerators =>
            ArgumentGeneratorCollections.ResolverArguments;

        protected abstract bool IsAsync { get; }

        protected override void GenerateDelegateHeader(
            string delegateName, T descriptor, StringBuilder source)
        {
            source.AppendLine($"/* {descriptor.Field.TypeName}.{descriptor.Field.FieldName} */");
            source.Append($"public static {nameof(FieldResolverDelegate)}");
            source.Append(" ");
            source.Append(delegateName);
            source.Append(" ");
            source.Append(" = ");
            if (IsAsync)
            {
                source.Append("async ctx => {");
            }
            else
            {
                source.Append("ctx => {");
            }
            source.AppendLine();
            source.AppendLine($"var ct = ctx.{nameof(IResolverContext.RequestAborted)};");
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

        protected virtual void HandleExceptionsSync(StringBuilder source, Action<StringBuilder> code)
        {
            source.AppendLine("try");
            source.AppendLine("{");
            code(source);
            source.AppendLine();
            source.AppendLine("}");
            source.AppendLine($"catch(HotChocolate.Execution.QueryException ex)");
            source.AppendLine("{");
            source.AppendLine($"return Task.FromResult<object>(ex.Errors);");
            source.AppendLine("}");
        }
    }
}
