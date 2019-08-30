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
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync($": IOperation<{descriptor.ResultType.Name}>");
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteLeftBraceAsync();
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteFieldsAsync(
                    writer, descriptor, typeLookup);
                await writer.WriteLineAsync();

                await WriteOperationPropertiesAsync(
                    writer, descriptor, typeLookup);
                await writer.WriteLineAsync();

                await WriteArgumentsAsync(
                    writer, descriptor, typeLookup);
                await writer.WriteLineAsync();

                await WriteVariablesAsync(
                    writer, descriptor, typeLookup);
            }

            await writer.WriteIndentAsync();
            await writer.WriteRightBraceAsync();
            await writer.WriteLineAsync();
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

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("private bool _isSet_");
                    await writer.WriteAsync(GetFieldName(argument.Name));
                    await writer.WriteAsync(';');
                    await writer.WriteLineAsync();
                }

                await writer.WriteLineAsync();

                for (int i = 0; i < descriptor.Arguments.Count; i++)
                {
                    Descriptors.IArgumentDescriptor argument =
                        descriptor.Arguments[i];

                    string typeName = typeLookup.GetTypeName(
                        argument.Type,
                        argument.Type.NamedType().Name,
                        true);

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("private ");
                    await writer.WriteAsync(typeName);
                    await writer.WriteSpaceAsync();
                    await writer.WriteAsync('_');
                    await writer.WriteAsync(GetFieldName(argument.Name));
                    await writer.WriteAsync(';');
                    await writer.WriteLineAsync();
                }
            }
        }

        private async Task WriteOperationPropertiesAsync(
            CodeWriter writer,
            IOperationDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public string Name => ");
            await writer.WriteStringValueAsync(descriptor.Operation.Name.Value);
            await writer.WriteAsync(';');
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("public IDocument Document => ");
            await writer.WriteAsync(descriptor.Query.Name);
            await writer.WriteAsync(".Default");
            await writer.WriteAsync(';');
            await writer.WriteLineAsync();
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
                    await writer.WriteLineAsync();
                }

                await WriteArgumentAsync(
                    writer,
                    descriptor.Arguments[i],
                    typeLookup);
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

            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ");
            await writer.WriteAsync(typeName);
            await writer.WriteSpaceAsync();
            await writer.WriteAsync(GetPropertyName(argument.Name));
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("get => ");
                await writer.WriteAsync('_');
                await writer.WriteAsync(GetFieldName(argument.Name));
                await writer.WriteAsync(';');
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync("set");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync('{');
                await writer.WriteLineAsync();

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync('_');
                    await writer.WriteAsync(GetFieldName(argument.Name));
                    await writer.WriteAsync(" = value;");
                    await writer.WriteLineAsync();

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("_isSet_");
                    await writer.WriteAsync(GetFieldName(argument.Name));
                    await writer.WriteAsync(" = true;");
                    await writer.WriteLineAsync();
                }

                await writer.WriteIndentAsync();
                await writer.WriteAsync('}');
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteVariablesAsync(
            CodeWriter writer,
            IOperationDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync(
                "public IReadOnlyList<VariableValue> GetVariableValues()");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "var variables = new List<VariableValue>();");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync();

                for (int i = 0; i < descriptor.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        await writer.WriteLineAsync();
                    }

                    await WriteVariableAsync(
                        writer,
                        descriptor.Arguments[i],
                        typeLookup);
                }

                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync("return variables;");
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteVariableAsync(
            CodeWriter writer,
            Descriptors.IArgumentDescriptor argument,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("if(_isSet_");
            await writer.WriteAsync(GetFieldName(argument.Name));
            await writer.WriteAsync(')');
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("variables.Add(new VariableValue(");
                await writer.WriteStringValueAsync(
                    argument.Name);
                await writer.WriteAsync(", ");
                await writer.WriteStringValueAsync(
                    argument.Type.NamedType().Name);
                await writer.WriteAsync(", ");
                await writer.WriteAsync(GetPropertyName(argument.Name));
                await writer.WriteAsync(')');
                await writer.WriteAsync(')');
                await writer.WriteAsync(';');
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }
    }
}
