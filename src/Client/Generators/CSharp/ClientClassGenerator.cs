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
                        new NonNullType(operation.OperationType),
                        operation.ResultType.Name,
                        true);

                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await WriteOperationAsync(
                        writer, operation, typeName, typeLookup)
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await WriteOperationRequestAsync(
                        writer, operation, typeName, typeLookup)
                        .ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static async Task WriteFieldsAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            await writer.WriteIndentedLineAsync(
                $"private const string _clientName = \"{descriptor.Name}\";")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

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

        private static async Task WriteConstructorAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            bool executor = descriptor.Operations.Any(t =>
                t.Operation.Operation != OperationType.Subscription);
            bool streamExecutor = descriptor.Operations.Any(t =>
                t.Operation.Operation == OperationType.Subscription);

            await writer.WriteIndentedLineAsync(
                $"public {GetClassName(descriptor.Name)}(IOperationExecutorPool executorPool)")
                .ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                if (executor)
                {
                    await writer.WriteIndentedLineAsync(
                        "_executor = executorPool.CreateExecutor(_clientName);")
                        .ConfigureAwait(false);
                }

                if (streamExecutor)
                {
                    await writer.WriteIndentedLineAsync(
                        "_streamExecutor = executorPool.CreateStreamExecutor(_clientName);")
                        .ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private static async Task WriteOperationAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            ITypeLookup typeLookup)
        {
            await WriteOperationSignatureAsync(
                writer, operation, operationTypeName, typeLookup)
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

        private static async Task WriteOperationRequestAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
            ITypeLookup typeLookup)
        {
            await WriteOperationRequestSignatureAsync(
                writer, operation, operationTypeName)
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync("if (operation is null)")
                    .ConfigureAwait(false);
                await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "throw new ArgumentNullException(nameof(operation));")
                        .ConfigureAwait(false);
                }
                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);

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

                await writer.WriteAsync("operation, cancellationToken);")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private static async Task WriteOperationSignatureAsync(
            CodeWriter writer,
            IOperationDescriptor operation,
            string operationTypeName,
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
                    await writer.WriteAsync($"Optional<{argumentType}>").ConfigureAwait(false);
                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                    await writer.WriteAsync(" = default").ConfigureAwait(false);
                }

                if (operation.Arguments.Count > 0)
                {
                    await writer.WriteAsync(',').ConfigureAwait(false);
                }
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "CancellationToken cancellationToken = default")
                    .ConfigureAwait(false);
                await writer.WriteAsync(')').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private static async Task WriteOperationRequestSignatureAsync(
           CodeWriter writer,
           IOperationDescriptor operation,
           string operationTypeName)
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
                $"{GetPropertyName(operation.Operation.Name!.Value)}Async(")
                .ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteLineAsync()
                        .ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(operation.Name).ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync("operation").ConfigureAwait(false);

                await writer.WriteAsync(',').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "CancellationToken cancellationToken = default")
                    .ConfigureAwait(false);
                await writer.WriteAsync(')').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private static async Task WriteOperationNullChecksAsync(
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

                if (needsNullCheck)
                {
                    if (checks > 0)
                    {
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    checks++;

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        $"if ({argument.Name}.HasValue && {argument.Name}.Value is null)")
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

        private static async Task WriteCreateOperationAsync(
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
                await writer.WriteAsync(" { ").ConfigureAwait(false);

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

                        if (i < operation.Arguments.Count - 1)
                        {
                            await writer.WriteAsync(',').ConfigureAwait(false);
                            await writer.WriteSpaceAsync().ConfigureAwait(false);
                        }

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
