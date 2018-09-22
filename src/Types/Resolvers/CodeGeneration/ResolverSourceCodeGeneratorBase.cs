using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class ResolverSourceCodeGeneratorBase<T>
        : SourceCodeGenerator<T>
        where T : IDelegateDescriptor
    {
        protected abstract IReadOnlyCollection<ArgumentSourceCodeGenerator> ArgumentGenerators { get; }

        protected override string Generate(
            string delegateName,
            T descriptor)
        {
            var source = new StringBuilder();

            GenerateDelegateHeader(delegateName, descriptor, source);

            GenerateArgumentDeclaration(descriptor, source);

            GenerateResolverInvocation(descriptor, source);

            GenerateDelegateFooter(delegateName, descriptor, source);

            return source.ToString();
        }

        protected abstract void GenerateDelegateHeader(
            string delegateName,
            T descriptor,
            StringBuilder source);

        protected abstract void GenerateDelegateFooter(
            string delegateName,
            T descriptor,
            StringBuilder source);

        protected void GenerateArguments(
            DirectiveMiddlewareDescriptor resolverDescriptor,
            StringBuilder source)
        {
            if (resolverDescriptor.Arguments.Count > 0)
            {
                string arguments = string.Join(", ",
                    resolverDescriptor.Arguments
                        .Select(t => t.VariableName));
                source.Append(arguments);
            }
        }

        protected virtual void GenerateArgumentDeclaration(
            T descriptor,
            StringBuilder source)
        {
            foreach (ArgumentDescriptor argumentDescriptor in
                GetArguments(descriptor))
            {
                GenerateArgumentInvocation(argumentDescriptor, source);
                source.AppendLine();
            }
        }

        protected abstract IEnumerable<ArgumentDescriptor> GetArguments(
            T descriptor);

        private void GenerateArgumentInvocation(
            ArgumentDescriptor argumentDescriptor,
            StringBuilder source)
        {
            source.Append($"var {argumentDescriptor.VariableName} = ");

            ArgumentSourceCodeGenerator generator = ArgumentGenerators
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

        protected void HandleExceptionsAsync(StringBuilder source, Action<StringBuilder> code)
        {
            source.AppendLine("try");
            source.AppendLine("{");
            code(source);
            source.AppendLine();
            source.AppendLine("}");
            source.AppendLine($"catch(HotChocolate.Execution.QueryException ex)");
            source.AppendLine("{");
            source.AppendLine($"return System.Threading.Tasks.Task.FromResult<object>(ex.Errors);");
            source.AppendLine("}");
        }
    }
}
