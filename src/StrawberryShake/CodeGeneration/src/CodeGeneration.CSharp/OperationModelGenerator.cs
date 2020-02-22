using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationModelGenerator
        : CodeGenerator<OperationModelDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            OperationModelDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ClassBuilder classBuilder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name)
                    .AddImplements($"global::StrawberryShake.IOperation<{descriptor.ResultType}>")
                    .AddProperty(PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetType("string")
                        .SetName("Name")
                        .SetGetter(CodeLineBuilder.New()
                            .SetLine($"return \"{descriptor.GraphQLName}\";")))
                    .AddProperty(PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetType("global::StrawberryShake.IDocument")
                        .SetName("Document")
                        .SetGetter(CodeLineBuilder.New()
                            .SetLine($"return {descriptor.DocumentType}.Default;")))
                    .AddProperty(PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetType("global::StrawberryShake.OperationKind")
                        .SetName("Kind")
                        .SetGetter(CodeLineBuilder.New()
                            .SetLine($"return OperationKind.{descriptor.OperationKind};")))
                    .AddProperty(PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetType("global::System.Type")
                        .SetName("ResultType")
                        .SetGetter(CodeLineBuilder.New()
                            .SetLine($"return typeof({descriptor.ResultType});")));

            AddConstructor(classBuilder, descriptor.Arguments);
            AddArgumentProperties(classBuilder, descriptor.Arguments);
            AddGetVariableValues(classBuilder, descriptor.Arguments, CodeWriter.Indent);

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private static void AddConstructor(
            ClassBuilder classBuilder,
            IReadOnlyList<OperationArgumentDescriptor> arguments)
        {
            if (arguments.Count == 0)
            {
                return;
            }

            ConstructorBuilder constructorBuilder =
                ConstructorBuilder.New()
                    .SetAccessModifier(AccessModifier.Public);

            foreach (OperationArgumentDescriptor argument in arguments.OrderBy(t => t.IsOptional))
            {
                constructorBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetType(argument.Type)
                        .SetName(argument.ParameterName)
                        .SetDefault(condition: argument.IsOptional));
            }

            constructorBuilder.AddCode(CreateConstructorBody(arguments));

            classBuilder.AddConstructor(constructorBuilder);
        }

        private static CodeBlockBuilder CreateConstructorBody(
            IReadOnlyList<OperationArgumentDescriptor> arguments)
        {
            var body = new StringBuilder();

            bool first = true;

            foreach (OperationArgumentDescriptor argument in arguments.OrderBy(t => t.IsOptional))
            {
                if (first)
                {
                    body.AppendLine();
                    first = false;
                }

                body.Append($"{arguments[0].Name} = {arguments[0].ParameterName};");
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private static void AddArgumentProperties(
            ClassBuilder classBuilder,
            IReadOnlyList<OperationArgumentDescriptor> arguments)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                classBuilder.AddProperty(
                    PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetType(arguments[i].Type)
                        .SetName(arguments[i].Name)
                        .MakeSettable());
            }
        }

        private static void AddGetVariableValues(
            ClassBuilder classBuilder,
            IReadOnlyList<OperationArgumentDescriptor> arguments,
            string indent)
        {
            MethodBuilder methodBuilder = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(
                    "global::System.Collections.Generic.IReadOnlyList<" +
                    "global::StrawberryShake.VariableValue>")
                .SetName("GetVariableValues")
                .AddCode(CreateGetVariableValuesBody(arguments, indent));
            classBuilder.AddMethod(methodBuilder);
        }

        private static CodeBlockBuilder CreateGetVariableValuesBody(
            IReadOnlyList<OperationArgumentDescriptor> arguments,
            string indent)
        {
            if (arguments.Count == 0)
            {
                return CodeBlockBuilder.New()
                    .AddCode(
                        "return global::System.Array.Empty<" +
                        "global::StrawberryShake.VariableValue>();");
            }

            var body = new StringBuilder();

            body.AppendLine("var variables = new List<VariableValue>();");
            body.AppendLine();

            foreach (OperationArgumentDescriptor argument in arguments)
            {
                if (argument.IsOptional)
                {
                    body.AppendLine("if (Episode.HasValue)");
                    body.AppendLine("{");
                    body.AppendLine(
                        $"{indent}variables.Add(new VariableValue(" +
                        $"\"{argument.GraphQLName}\", " +
                        $"\"{argument.GraphQLType}\", " +
                        $"{argument.Name}.Value));");
                    body.AppendLine("}");
                }
                else
                {
                    body.AppendLine(
                        $"variables.Add(new VariableValue(" +
                        $"\"{argument.GraphQLName}\", " +
                        $"\"{argument.GraphQLType}\", " +
                        $"{argument.Name}));");
                }
                body.AppendLine();
            }

            body.Append("return variables;");

            return CodeBlockBuilder.FromStringBuilder(body);
        }
    }
}
