using System;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClientClassGenerator
        : CodeGenerator<ClientClassDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            ClientClassDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ClassBuilder classBuilder = ClassBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName(descriptor.Name)
                .AddImplements(descriptor.InterfaceName);

            classBuilder.AddField(
                FieldBuilder.New()
                    .SetConst()
                    .SetName("_clientName")
                    .SetType("string")
                    .SetValue($"\"{descriptor.Name}\""));

            ConstructorBuilder constructor = ConstructorBuilder.New()
                .AddParameter(
                    ParameterBuilder.New()
                        .SetName("executorPool")
                        .SetType(descriptor.OperationExecutorPool));

            if (descriptor.OperationExecutor is { })
            {
                classBuilder.AddField(
                    FieldBuilder.New()
                        .SetReadOnly()
                        .SetName("_executor")
                        .SetType(descriptor.OperationExecutor));
                constructor
                    .AddCode("_executor = executorPool.CreateExecutor(_clientName);");
            }

            if (descriptor.OperationStreamExecutor is { })
            {
                classBuilder.AddField(
                    FieldBuilder.New()
                        .SetReadOnly()
                        .SetName("_streamExecutor")
                        .SetType(descriptor.OperationStreamExecutor));
                constructor
                    .AddCode("_streamExecutor = executorPool.CreateStreamExecutor(_clientName);");
            }

            classBuilder.AddConstructor(constructor);

            foreach (ClientOperationMethodDescriptor operation in descriptor.Operations)
            {
                string returnType = CreateReturnType(
                    operation.ResponseModelName,
                    operation.IsStreamExecutor);

                MethodBuilder methodBuilder = MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(operation.Name + "Async")
                    .SetReturnType(returnType);

                foreach (ClientOperationMethodParameterDescriptor parameter in operation.Parameters)
                {
                    ParameterBuilder parameterBuilder = ParameterBuilder.New()
                        .SetName(parameter.Name)
                        .SetType(parameter.TypeName);

                    if (parameter.IsOptional && parameter.Default is null)
                    {
                        parameterBuilder.SetDefault();
                    }

                    if (parameter.Default is { })
                    {
                        parameterBuilder.SetDefault(parameter.Default);
                    }

                    methodBuilder.AddParameter(parameterBuilder);
                }

                methodBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetName("cancellationToken")
                        .SetType("global::System.Threading.CancellationToken")
                        .SetDefault());

                methodBuilder.AddCode(CreateExecuteMethodCode(operation, CodeWriter.Indent));

                classBuilder.AddMethod(methodBuilder);

                methodBuilder = MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(operation.Name + "Async")
                    .SetReturnType(returnType);

                methodBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetName("operation")
                        .SetType(operation.OperationModelName));

                methodBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetName("cancellationToken")
                        .SetType("global::System.Threading.CancellationToken")
                        .SetDefault());

                methodBuilder.AddCode(CreateExecuteOperationMethodCode(CodeWriter.Indent));

                classBuilder.AddMethod(methodBuilder);
            }

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private CodeBlockBuilder CreateExecuteMethodCode(
            ClientOperationMethodDescriptor operation,
            string indent)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("return _executor.ExecuteAsync(");

            if (operation.Parameters.Count == 0)
            {
                stringBuilder.Append(
                    $"new {operation.OperationModelName}(), cancellationToken);");
            }
            else if (operation.Parameters.Count == 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(
                    $"{indent}new {operation.OperationModelName}{{ " +
                    $"{operation.Parameters[0].PropertyName} = " +
                    $"{operation.Parameters[0].Name} }},");
                stringBuilder.Append("cancellationToken);");
            }
            else
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{indent}new {operation.OperationModelName}");
                stringBuilder.AppendLine($"{indent}{{");

                for (int i = 0; i < operation.Parameters.Count; i++)
                {
                    ClientOperationMethodParameterDescriptor parameter = operation.Parameters[i];
                    stringBuilder.Append(
                        $"{indent}{indent}{parameter.PropertyName} = {parameter.Name}");

                    if (i + 1 < operation.Parameters.Count)
                    {
                        stringBuilder.AppendLine(",");
                    }
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{indent}}},");
                stringBuilder.Append($"{indent}cancellationToken);");
            }

            return CodeBlockBuilder.FromStringBuilder(stringBuilder);
        }

        private static CodeBlockBuilder CreateExecuteOperationMethodCode(string indent)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("if (operation is null)");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine(
                $"{indent}throw new ArgumentNullException(nameof(operation));");
            stringBuilder.AppendLine("}");
            stringBuilder.AppendLine();
            stringBuilder.Append("return _executor.ExecuteAsync(operation, cancellationToken);");

            return CodeBlockBuilder.FromStringBuilder(stringBuilder);
        }

        private static string CreateReturnType(string responseModelName, bool isStream)
        {
            if (isStream)
            {
                return "global::System.Threading.Tasks.Task<" +
                    $"global::StrawberryShale.IResponseStream<{responseModelName}>>";
            }
            else
            {
                return "global::System.Threading.Tasks.Task<" +
                    $"global::StrawberryShale.IOperationResult<{responseModelName}>>";
            }
        }
    }
}
