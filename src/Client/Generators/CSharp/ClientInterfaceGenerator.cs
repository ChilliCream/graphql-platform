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
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public interface ").ConfigureAwait(false);
            await writer.WriteAsync(GetInterfaceName(descriptor.Name)).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("{").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Operations.Count; i++)
                {
                    IOperationDescriptor operation = descriptor.Operations[i];

                    string typeName = typeLookup.GetTypeName(
                        new NonNullType(operation.OperationType),
                        operation.ResultType.Name,
                        true);

                    if (i > 0)
                    {
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await WriteOperationAsync(
                        writer, operation, typeName, true, typeLookup)
                        .ConfigureAwait(false);

                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await WriteOperationRequestAsync(
                        writer, operation, typeName, true)
                        .ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static async Task WriteOperationAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            bool cancellationToken,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            if (operation.Operation.Operation == OperationType.Subscription)
            {
                await writer.WriteAsync(
                    $"Task<IResponseStream<{operationTypeName}>> ")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteAsync(
                    $"Task<IOperationResult<{operationTypeName}>> ")
                    .ConfigureAwait(false);
            }
            await writer.WriteAsync(
                $"{GetPropertyName(operation.Operation.Name!.Value)}Async(")
                .ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                for (int j = 0; j < operation.Arguments.Count; j++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        operation.Arguments[j];

                    if (j > 0)
                    {
                        await writer.WriteAsync(',').ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync().ConfigureAwait(false);

                    string argumentType = typeLookup.GetTypeName(
                        argument.Type,
                        argument.Type.NamedType().Name,
                        true);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync($"Optional<{argumentType}>").ConfigureAwait(false);
                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(argument.Name))
                        .ConfigureAwait(false);
                    await writer.WriteAsync(" = default")
                        .ConfigureAwait(false);
                }

                if (cancellationToken)
                {
                    if (operation.Arguments.Count > 0)
                    {
                        await writer.WriteAsync(',').ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "CancellationToken cancellationToken = default")
                        .ConfigureAwait(false);
                }

                await writer.WriteAsync(')').ConfigureAwait(false);
                await writer.WriteAsync(';').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private static async Task WriteOperationRequestAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            bool cancellationToken)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            if (operation.Operation.Operation == OperationType.Subscription)
            {
                await writer.WriteAsync(
                    $"Task<IResponseStream<{operationTypeName}>> ")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteAsync(
                    $"Task<IOperationResult<{operationTypeName}>> ")
                    .ConfigureAwait(false);
            }
            await writer.WriteAsync(
                $"{GetPropertyName(operation.Operation.Name!.Value)}Async(")
                .ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(operation.Name).ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync("operation")
                    .ConfigureAwait(false);

                if (cancellationToken)
                {
                    await writer.WriteAsync(',').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "CancellationToken cancellationToken = default")
                        .ConfigureAwait(false);
                }

                await writer.WriteAsync(')').ConfigureAwait(false);
                await writer.WriteAsync(';').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }
    }
}
