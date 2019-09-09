using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class OperationGenerator
        : CodeGenerator<IOperationDescriptor>
    {
        protected override async Task WriteAsync(
            CodeWriter writer,
            IOperationDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public class ").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    $": IOperation<{descriptor.ResultType.Name}>")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteLeftBraceAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                if (descriptor.Arguments.Count > 0)
                {
                    await WriteFieldsAsync(
                        writer, descriptor, typeLookup)
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await WriteOperationPropertiesAsync(
                    writer, descriptor, typeLookup)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                if (descriptor.Arguments.Count > 0)
                {
                    await WriteArgumentsAsync(
                        writer, descriptor, typeLookup)
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await WriteVariablesAsync(
                    writer, descriptor, typeLookup)
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteRightBraceAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteFieldsAsync(
            CodeWriter writer,
            IOperationDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            if (descriptor.Arguments.Count > 0)
            {
                for (int i = 0; i < descriptor.Arguments.Count; i++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        descriptor.Arguments[i];

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("private bool _modified_").ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                    await writer.WriteAsync(';').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteLineAsync().ConfigureAwait(false);

                for (int i = 0; i < descriptor.Arguments.Count; i++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        descriptor.Arguments[i];

                    string typeName = typeLookup.GetTypeName(
                        argument.Type,
                        argument.Type.NamedType().Name,
                        true);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("private ").ConfigureAwait(false);
                    await writer.WriteAsync(typeName).ConfigureAwait(false);
                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                    await writer.WriteAsync("_value_").ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                    await writer.WriteAsync(';').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task WriteOperationPropertiesAsync(
            CodeWriter writer,
            IOperationDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public string Name => ").ConfigureAwait(false);
            await writer.WriteStringValueAsync(
                descriptor.Operation.Name.Value)
                .ConfigureAwait(false);
            await writer.WriteAsync(';').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public IDocument Document => ").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Query.Name).ConfigureAwait(false);
            await writer.WriteAsync(".Default").ConfigureAwait(false);
            await writer.WriteAsync(';').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public Type ResultType => ").ConfigureAwait(false);
            await writer.WriteAsync(
                $"typeof({descriptor.ResultType.Name});")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteArgumentsAsync(
            CodeWriter writer,
            IOperationDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            for (int i = 0; i < descriptor.Arguments.Count; i++)
            {
                if (i > 0)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await WriteArgumentAsync(
                    writer,
                    descriptor.Arguments[i],
                    typeLookup)
                    .ConfigureAwait(false);
            }
        }

        private async Task WriteArgumentAsync(
            CodeWriter writer,
            Descriptors.IArgumentDescriptor argument,
            ITypeLookup typeLookup)
        {
            string typeName = typeLookup.GetTypeName(
                argument.Type,
                argument.Type.NamedType().Name,
                true);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public ").ConfigureAwait(false);
            await writer.WriteAsync(typeName).ConfigureAwait(false);
            await writer.WriteSpaceAsync().ConfigureAwait(false);
            await writer.WriteAsync(GetPropertyName(argument.Name)).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("get => ").ConfigureAwait(false);
                await writer.WriteAsync("_value_").ConfigureAwait(false);
                await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                await writer.WriteAsync(';').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("set").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync('{').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("_value_").ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                    await writer.WriteAsync(" = value;").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("_modified_").ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
                    await writer.WriteAsync(" = true;").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync('}').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteVariablesAsync(
            CodeWriter writer,
            IOperationDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(
                "public IReadOnlyList<VariableValue> GetVariableValues()")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                if (descriptor.Arguments.Count == 0)
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "return Array.Empty<VariableValue>();")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "var variables = new List<VariableValue>();")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    for (int i = 0; i < descriptor.Arguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }

                        await WriteVariableAsync(
                            writer,
                            descriptor.Arguments[i],
                            typeLookup)
                            .ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("return variables;").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteVariableAsync(
            CodeWriter writer,
            Descriptors.IArgumentDescriptor argument,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("if(_modified_").ConfigureAwait(false);
            await writer.WriteAsync(GetFieldName(argument.Name)).ConfigureAwait(false);
            await writer.WriteAsync(')').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("variables.Add(new VariableValue(").ConfigureAwait(false);
                await writer.WriteStringValueAsync(argument.Name).ConfigureAwait(false);
                await writer.WriteAsync(", ").ConfigureAwait(false);
                await writer.WriteStringValueAsync(
                    argument.Type.NamedType().Name)
                    .ConfigureAwait(false);
                await writer.WriteAsync(", ").ConfigureAwait(false);
                await writer.WriteAsync(GetPropertyName(argument.Name)).ConfigureAwait(false);
                await writer.WriteAsync(')').ConfigureAwait(false);
                await writer.WriteAsync(')').ConfigureAwait(false);
                await writer.WriteAsync(';').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
