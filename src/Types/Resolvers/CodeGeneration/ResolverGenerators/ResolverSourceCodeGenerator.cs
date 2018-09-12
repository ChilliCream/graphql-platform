using System;
using System.Linq;
using System.Text;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class ResolverSourceCodeGenerator<T>
        : SourceCodeGenerator<T>
        where T : IFieldResolverDescriptor
    {
        protected override string Generate(
            string delegateName,
            T descriptor)
        {
            var source = new StringBuilder();
            source.AppendLine($"/* {descriptor.Field.TypeName}.{descriptor.Field.FieldName} */");
            source.Append($"public static {nameof(FieldResolverDelegate)}");
            source.Append(" ");
            source.Append(delegateName);
            source.Append(" ");
            source.Append(" = ");
            source.Append("(ctx, ct) => {");
            source.AppendLine();

            foreach (ArgumentDescriptor argumentDescriptor in
                descriptor.Arguments)
            {
                GenerateArgumentInvocation(argumentDescriptor, source);
                source.AppendLine();
            }

            GenerateResolverInvocation(descriptor, source);

            source.AppendLine();
            source.Append("};");
            return source.ToString();
        }

        private void GenerateArgumentInvocation(
            ArgumentDescriptor argumentDescriptor,
            StringBuilder source)
        {
            source.Append($"var {argumentDescriptor.VariableName} = ");

            ArgumentSourceCodeGenerator generator =
                ArgumentGeneratorCollections.ResolverArguments
                    .FirstOrDefault(t => t.CanHandle(argumentDescriptor));
            if (generator == null)
            {
                throw new NotSupportedException();
            }

            source.Append(generator.Generate(
                argumentDescriptor.VariableName,
                argumentDescriptor));

            source.Append(";");
        }

        protected abstract void GenerateResolverInvocation(
            T resolverDescriptor,
            StringBuilder source);

        protected string GetTypeName(Type type)
        {
            return type.GetTypeName();
        }

        protected void HandleExceptions(StringBuilder source, Action<StringBuilder> code)
        {
            source.AppendLine("try");
            source.AppendLine("{");
            code(source);
            source.AppendLine();
            source.AppendLine("}");
            source.AppendLine($"catch(HotChocolate.Execution.QueryException ex)");
            source.AppendLine("{");
            source.AppendLine($"return ex.Errors;");
            source.AppendLine("}");
        }
    }

}
