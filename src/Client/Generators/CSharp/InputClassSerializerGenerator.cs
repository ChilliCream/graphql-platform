using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using IInputFieldDescriptor = StrawberryShake.Generators.Descriptors.IInputFieldDescriptor;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class InputClassSerializerGenerator
        : CodeGenerator<IInputClassDescriptor>
    {
        protected override string CreateFileName(
            IInputClassDescriptor descriptor)
        {
            return descriptor.Name + "Serializer.cs";
        }

        protected override async Task WriteAsync(
            CodeWriter writer,
            IInputClassDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteAsync("Serializer");
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync(" : IValueSerializer");
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteLeftBraceAsync();
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteSerializerFieldsAsync(writer, descriptor);
                await WriteConstructorAsync(writer, descriptor);
                await WriteProperties(writer, descriptor);
                await WriteSerializeMethod(writer, descriptor);
            }

            await writer.WriteRightBraceAsync();
            await writer.WriteLineAsync();
        }

        private async Task WriteSerializerFieldsAsync(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            foreach (IInputFieldDescriptor field in descriptor.Fields)
            {
                string typeName = field.InputObjectType is null
                    ? field.Type.NamedType().Name.Value
                    : field.InputObjectType.Name;

                await writer.WriteIndentAsync();
                await writer.WriteAsync("private readonly IValueSerializer");
                await writer.WriteAsync('_');
                await writer.WriteAsync(GetFieldName(typeName));
                await writer.WriteAsync("Serializer");
                await writer.WriteAsync(';');
                await writer.WriteLineAsync();
            }
        }

        private async Task WriteConstructorAsync(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteAsync("Serializer");
            await writer.WriteAsync("(IEnumerable<IValueSerializer> serializers)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("IReadOnlyDictionary<string, IValueSerializer> map = ");
                await writer.WriteAsync("serializers.ToDictionary();");
                await writer.WriteLineAsync();

                foreach (IInputFieldDescriptor field in descriptor.Fields)
                {
                    string typeName = field.InputObjectType is null
                        ? field.Type.NamedType().Name.Value
                        : field.InputObjectType.Name;

                    await writer.WriteLineAsync();
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(
                        "if (!map.TryGetValue" +
                        $"(\"{typeName}\", out IValueSerializer serializer))");
                    await writer.WriteAsync('{');
                    await writer.WriteLineAsync();

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync();
                        await writer.WriteAsync("throw new ArgumentException(");
                        await writer.WriteLineAsync();

                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentAsync();
                            await writer.WriteAsync(
                                "\"There is no serializer specified for " +
                                $"`{typeName}`.\",");
                            await writer.WriteLineAsync();

                            await writer.WriteIndentAsync();
                            await writer.WriteAsync("nameof(serializers));");
                            await writer.WriteLineAsync();
                        }
                    }

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync('}');
                    await writer.WriteLineAsync();

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync('_');
                    await writer.WriteAsync(GetFieldName(typeName));
                    await writer.WriteAsync("Serializer = serializer;");
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteProperties(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public string Name { get } =");
            await writer.WriteStringValueAsync(descriptor.Name);
            await writer.WriteAsync(';');
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ValueKind Kind { get } = ValueKind.InputObject;");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ValueKind Kind { get } = ");
            await writer.WriteAsync("typeof(");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteAsync(");");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();
        }

        private async Task WriteSerializeMethod(
            CodeWriter writer,
            IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public object Serialize(object value)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("if(value is null)");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync('{');
                await writer.WriteLineAsync();

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("return null;");
                    await writer.WriteLineAsync();
                }

                await writer.WriteIndentAsync();
                await writer.WriteAsync('}');
                await writer.WriteLineAsync();

                // TODO : we need to handle loops
                await writer.WriteIndentAsync();
                await writer.WriteAsync($"var input = ({descriptor.Name})value;");
                await writer.WriteAsync("var map = new Dictionary<string, object>();");
                await writer.WriteLineAsync();

                foreach (IInputFieldDescriptor field in descriptor.Fields)
                {
                    string typeName = field.InputObjectType is null
                        ? field.Type.NamedType().Name.Value
                        : field.InputObjectType.Name;

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync($"map[\"{field.Field.Name}\"] = ");
                    await writer.WriteAsync('_');
                    await writer.WriteAsync(GetFieldName(typeName));
                    await writer.WriteAsync("Serializer.Serialize(");
                    await writer.WriteAsync($"input.{field.Name});");
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }
    }
}
