using System.Collections.Generic;
using System.Linq;
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
        , IUsesComponents
    {
        public IReadOnlyList<string> Components { get; } =
            new List<string>
            {
                WellKnownComponents.Task
            };

        protected override async Task WriteAsync(
            CodeWriter writer,
            IClientDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public class ").ConfigureAwait(false);
            await writer.WriteAsync(GetClassName(descriptor.Name)).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(": ").ConfigureAwait(false);
                await writer.WriteAsync(GetInterfaceName(descriptor.Name)).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("{").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await WriteFieldsAsync(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteConstructorAsync(writer, descriptor).ConfigureAwait(false);

                for (int i = 0; i < descriptor.Operations.Count; i++)
                {
                    IOperationDescriptor operation = descriptor.Operations[i];

                    string typeName = typeLookup.GetTypeName(
                        operation.OperationType,
                        operation.ResultType.Name,
                        true);

                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await WriteOperationOverloadAsync(
                        writer, operation, typeName, typeLookup)
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await WriteOperationAsync(
                        writer, operation, typeName, typeLookup)
                        .ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteFieldsAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            if (descriptor.Operations.Any(
                t => t.Operation.Operation != OperationType.Subscription))
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "private readonly IOperationExecutor _executor;")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            if (descriptor.Operations.Any(
                t => t.Operation.Operation == OperationType.Subscription))
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "private readonly IOperationStreamExecutor _streamExecutor;")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteConstructorAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public ").ConfigureAwait(false);
            await writer.WriteAsync(GetClassName(descriptor.Name)).ConfigureAwait(false);
            await writer.WriteAsync("(").ConfigureAwait(false);

            bool executor = false;
            bool streamExecutor = false;

            if (descriptor.Operations.Any(
                t => t.Operation.Operation != OperationType.Subscription))
            {
                executor = true;

                await writer.WriteAsync("IOperationExecutor executor").ConfigureAwait(false);
            }

            if (descriptor.Operations.Any(
                t => t.Operation.Operation == OperationType.Subscription))
            {
                streamExecutor = true;

                if (executor)
                {
                    await writer.WriteAsync(", ").ConfigureAwait(false);
                }
                await writer.WriteAsync("IOperationStreamExecutor streamExecutor")
                    .ConfigureAwait(false);
            }

            await writer.WriteAsync(')').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                if (executor)
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("_executor = executor " +
                        "?? throw new ArgumentNullException(nameof(executor));")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync()
                        .ConfigureAwait(false);
                }

                if (streamExecutor)
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("_streamExecutor = streamExecutor " +
                        "?? throw new ArgumentNullException(nameof(streamExecutor));")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync()
                        .ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteOperationOverloadAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            ITypeLookup typeLookup)
        {
            await WriteOperationSignature(
                writer, operation, operationTypeName, false, typeLookup)
                .ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    $"{GetPropertyName(operation.Operation.Name.Value)}Async(")
                    .ConfigureAwait(false);

                for (int j = 0; j < operation.Arguments.Count; j++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        operation.Arguments[j];

                    if (j > 0)
                    {
                        await writer.WriteAsync(',').ConfigureAwait(false);
                        await writer.WriteSpaceAsync().ConfigureAwait(false);
                    }

                    await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                }

                if (operation.Arguments.Count > 0)
                {
                    await writer.WriteAsync(',').ConfigureAwait(false);
                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                }

                await writer.WriteAsync("CancellationToken.None").ConfigureAwait(false);
                await writer.WriteAsync(')').ConfigureAwait(false);
                await writer.WriteAsync(';').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteOperationAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            ITypeLookup typeLookup)
        {
            await WriteOperationSignature(
                writer, operation, operationTypeName, true, typeLookup)
                .ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await WriteOperationNullChecksAsync(
                    writer, operation, typeLookup)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                if (operation.Operation.Operation == OperationType.Subscription)
                {
                    await writer.WriteAsync("return _streamExecutor.ExecuteAsync(")
                        .ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteAsync("return _executor.ExecuteAsync(")
                        .ConfigureAwait(false);
                }
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    await WriteCreateOperationAsync(
                        writer, operation, typeLookup)
                        .ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("cancellationToken);").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteOperationSignature(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            bool cancellationToken,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public ").ConfigureAwait(false);
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
                $"{GetPropertyName(operation.Operation.Name.Value)}Async(")
                .ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                for (int j = 0; j < operation.Arguments.Count; j++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        operation.Arguments[j];

                    if (j > 0)
                    {
                        await writer.WriteAsync(',')
                            .ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync()
                        .ConfigureAwait(false);

                    string argumentType = typeLookup.GetTypeName(
                        argument.Type,
                        argument.Type.NamedType().Name,
                        true);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(argumentType).ConfigureAwait(false);
                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                }

                if (cancellationToken)
                {
                    if (operation.Arguments.Count > 0)
                    {
                        await writer.WriteAsync(',').ConfigureAwait(false);
                    }
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("CancellationToken cancellationToken")
                        .ConfigureAwait(false);
                    await writer.WriteAsync(')').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteAsync(") =>").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
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
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    checks++;

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync($"if ({argument.Name} is null)")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('{').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync(
                            $"throw new ArgumentNullException(nameof({argument.Name}));")
                            .ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('}').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task WriteCreateOperationAsync(
           CodeWriter writer,
           IOperationDescriptor operation,
           ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("new ").ConfigureAwait(false);
            await writer.WriteAsync(operation.Name).ConfigureAwait(false);

            if (operation.Arguments.Count == 0)
            {
                await writer.WriteAsync("(),").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
            else if (operation.Arguments.Count == 1)
            {
                await writer.WriteAsync(" {").ConfigureAwait(false);

                Descriptors.IArgumentDescriptor argument =
                    operation.Arguments[0];

                await writer.WriteAsync(GetPropertyName(argument.Name)).ConfigureAwait(false);
                await writer.WriteAsync(" = ").ConfigureAwait(false);
                await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);

                await writer.WriteAsync(" },").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
            else
            {
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync('{').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    for (int i = 0; i < operation.Arguments.Count; i++)
                    {
                        Descriptors.IArgumentDescriptor argument =
                            operation.Arguments[i];

                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync(GetPropertyName(argument.Name))
                            .ConfigureAwait(false);
                        await writer.WriteAsync(" = ").ConfigureAwait(false);
                        await writer.WriteAsync(GetFieldName(argument.Name))
                            .ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                }

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("},").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }
    }
}
