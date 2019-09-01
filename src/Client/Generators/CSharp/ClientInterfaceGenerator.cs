using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ClientInterfaceGenerator
        : CodeGenerator<IClientDescriptor>
        , IUsesComponents
    {
        public IReadOnlyList<string> Components { get; } =
            new List<string>
            {
                WellKnownComponents.Task
            };

        protected override string CreateFileName(IClientDescriptor descriptor)
        {
            return GetInterfaceName(descriptor.Name) + ".cs";
        }

        protected override async Task WriteAsync(
            CodeWriter writer,
            IClientDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public interface ");
            await writer.WriteAsync(GetInterfaceName(descriptor.Name));
            await writer.WriteLineAsync();

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

                    await WriteOperationAsync(
                        writer, operation, typeName, false, typeLookup);

                    await writer.WriteLineAsync();

                    await WriteOperationAsync(
                        writer, operation, typeName, true, typeLookup);
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
            await writer.WriteLineAsync();
        }

        private async Task WriteOperationAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            bool cancellationToken,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
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
                $"{GetPropertyName(operation.Operation.Name.Value)}Async(");

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
                }

                await writer.WriteAsync(')');
                await writer.WriteAsync(';');
                await writer.WriteLineAsync();
            }
        }
    }
}
