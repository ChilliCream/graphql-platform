using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ClientClassGenerator
        : CodeGenerator<IClientDescriptor>
    {
        protected override async Task WriteAsync(
            CodeWriter writer,
            IClientDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(GetClassName(descriptor.Name));
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync(": ");
                await writer.WriteAsync(GetInterfaceName(descriptor.Name));
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Operations.Count; i++)
                {
                    IOperationDescriptor operation = descriptor.Operations[i];

                    string typeName = typeLookup.GetTypeName(
                        operation.OperationType,
                        operation.ResultType.Name,
                        true);

                    if (i > 0)
                    {
                        await writer.WriteLineAsync();
                    }

                    await WriteOperationOverloadAsync(
                        writer, operation, typeName, typeLookup);

                    await WriteOperationAsync(
                        writer, operation, typeName, typeLookup);
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
            await writer.WriteLineAsync();
        }

        private async Task WriteOperationOverloadAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            ITypeLookup typeLookup)
        {
            await WriteOperationSignature(
                writer, operation, operationTypeName, true, typeLookup);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync($"{operation.Operation.Name.Value}Async(");

                for (int j = 0; j < operation.Arguments.Count; j++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        operation.Arguments[j];

                    if (j > 0)
                    {
                        await writer.WriteAsync(',');
                    }

                    await writer.WriteLineAsync();

                    string argumentType = typeLookup.GetTypeName(
                        argument.Type,
                        argument.Type.NamedType().Name,
                        true);

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(argumentType);
                    await writer.WriteSpaceAsync();
                    await writer.WriteAsync(GetFieldName(argument.Name));
                }

                if (operation.Arguments.Count > 0)
                {
                    await writer.WriteAsync(',');
                }
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync("CancellationToken.None");
                await writer.WriteAsync(')');
                await writer.WriteAsync(';');
                await writer.WriteLineAsync();
            }
        }

        private async Task WriteOperationAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            ITypeLookup typeLookup)
        {
            await WriteOperationSignature(
                writer, operation, operationTypeName, true, typeLookup);

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteOperationNullChecksAsync(
                    writer, operation, typeLookup);
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                if (operation.Operation.Operation == OperationType.Subscription)
                {
                    await writer.WriteAsync("return _streamExecutor.ExecuteAsync(");
                }
                else
                {
                    await writer.WriteAsync("return _executor.ExecuteAsync(");
                }
                using (writer.IncreaseIndent())
                {
                    await WriteCreateOperationAsync(
                        writer, operation, typeLookup);

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("cancellationToken);");
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteOperationSignature(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            bool cancellationToken,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ");
            if (operation.Operation.Operation == OperationType.Subscription)
            {
                await writer.WriteAsync(
                    $"Task<IResponseStream<{operationTypeName}>> ");
            }
            else
            {
                await writer.WriteAsync(
                    $"Task<IOperationResult<{operationTypeName}>> ");
            }
            await writer.WriteAsync(
                $"{operation.Operation.Name.Value}Async(");

            using (writer.IncreaseIndent())
            {
                for (int j = 0; j < operation.Arguments.Count; j++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        operation.Arguments[j];

                    if (j > 0)
                    {
                        await writer.WriteAsync(',');
                    }

                    await writer.WriteLineAsync();

                    string argumentType = typeLookup.GetTypeName(
                        argument.Type,
                        argument.Type.NamedType().Name,
                        true);

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(argumentType);
                    await writer.WriteSpaceAsync();
                    await writer.WriteAsync(GetFieldName(argument.Name));
                }

                if (cancellationToken)
                {
                    if (operation.Arguments.Count > 0)
                    {
                        await writer.WriteAsync(',');
                    }
                    await writer.WriteLineAsync();

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("CancellationToken cancellationToken");
                    await writer.WriteAsync(')');
                    await writer.WriteLineAsync();
                }
                else
                {
                    await writer.WriteAsync(") =>");
                    await writer.WriteLineAsync();
                }
            }
        }

        private async Task WriteOperationNullChecksAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            ITypeLookup typeLookup)
        {
            int checks = 0;

            for (int j = 0; j < operation.Arguments.Count; j++)
            {
                Descriptors.IArgumentDescriptor argument =
                    operation.Arguments[j];

                bool needsNullCheck = argument.Type.IsNonNullType();

                if (needsNullCheck && argument.Type.IsLeafType())
                {
                    ITypeInfo argumentType = typeLookup.GetTypeInfo(
                        argument.Type,
                        true);
                    needsNullCheck = !argumentType.IsValueType;
                }

                if (argument.Type.IsNonNullType())
                {
                    if (checks > 0)
                    {
                        await writer.WriteLineAsync();
                    }

                    checks++;

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync($"if ({argument.Name} is null)");
                    await writer.WriteLineAsync();

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync('{');
                    await writer.WriteLineAsync();

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync();
                        await writer.WriteAsync(
                            $"throw new ArgumentNullException(nameof({argument.Name}));");
                        await writer.WriteLineAsync();
                    }

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync('}');
                    await writer.WriteLineAsync();
                }
            }
        }

        private async Task WriteCreateOperationAsync(
           CodeWriter writer,
           IOperationDescriptor operation,
           ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("new ");
            await writer.WriteAsync(operation.Name);

            if (operation.Arguments.Count == 0)
            {
                await writer.WriteAsync("(),");
                await writer.WriteLineAsync();
            }
            else if (operation.Arguments.Count == 1)
            {
                await writer.WriteAsync(" {");

                Descriptors.IArgumentDescriptor argument =
                    operation.Arguments[0];

                await writer.WriteAsync(GetPropertyName(argument.Name));
                await writer.WriteAsync(" = ");
                await writer.WriteAsync(GetFieldName(argument.Name));

                await writer.WriteAsync(" },");
                await writer.WriteLineAsync();
            }
            else
            {
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync('{');
                await writer.WriteLineAsync();

                using (writer.IncreaseIndent())
                {
                    for (int i = 0; i < operation.Arguments.Count; i++)
                    {
                        Descriptors.IArgumentDescriptor argument =
                            operation.Arguments[i];

                        await writer.WriteIndentAsync();
                        await writer.WriteAsync(GetPropertyName(argument.Name));
                        await writer.WriteAsync(" = ");
                        await writer.WriteAsync(GetFieldName(argument.Name));
                        await writer.WriteLineAsync();
                    }
                }

                await writer.WriteIndentAsync();
                await writer.WriteAsync("},");
                await writer.WriteLineAsync();
            }
        }
    }
}
