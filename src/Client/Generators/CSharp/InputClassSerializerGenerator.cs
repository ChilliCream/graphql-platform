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
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public class ").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteAsync("Serializer").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(": IValueSerializer").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteLeftBraceAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await WriteSerializerFieldsAsync(writer, descriptor)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteConstructorAsync(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteProperties(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteSerializeMethod(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteDeserializeMethod(writer, descriptor).ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteRightBraceAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
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

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("private readonly IValueSerializer")
                    .ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync('_').ConfigureAwait(false);
                await writer.WriteAsync(GetFieldName(typeName)).ConfigureAwait(false);
                await writer.WriteAsync("Serializer").ConfigureAwait(false);
                await writer.WriteAsync(';').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteConstructorAsync(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public ").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteAsync("Serializer").ConfigureAwait(false);
            await writer.WriteAsync("(IEnumerable<IValueSerializer> serializers)")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("IReadOnlyDictionary<string, IValueSerializer> map = ")
                    .ConfigureAwait(false);
                await writer.WriteAsync("serializers.ToDictionary();")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                for (int i = 0; i < descriptor.Fields.Count; i++)
                {
                    IInputFieldDescriptor field = descriptor.Fields[i];

                    string typeName = field.InputObjectType is null
                        ? field.Type.NamedType().Name.Value
                        : field.InputObjectType.Name;

                    string serializerType = i == 0
                        ? "IValueSerializer "
                        : string.Empty;

                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "if (!map.TryGetValue" +
                        $"(\"{typeName}\", out {serializerType}serializer))")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('{').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync("throw new ArgumentException(")
                            .ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);

                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentAsync().ConfigureAwait(false);
                            await writer.WriteAsync(
                                "\"There is no serializer specified for " +
                                $"`{typeName}`.\",")
                                .ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);

                            await writer.WriteIndentAsync().ConfigureAwait(false);
                            await writer.WriteAsync("nameof(serializers));")
                                .ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                    }

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('}').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('_').ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(typeName)).ConfigureAwait(false);
                    await writer.WriteAsync("Serializer = serializer;").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteProperties(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public string Name { get; } = ").ConfigureAwait(false);
            await writer.WriteStringValueAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteAsync(';').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(
                "public ValueKind Kind { get; } = " +
                "ValueKind.InputObject;")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public Type ClrType => ").ConfigureAwait(false);
            await writer.WriteAsync("typeof(").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteAsync(");").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public Type SerializationType => ")
                .ConfigureAwait(false);
            await writer.WriteAsync("typeof(IReadOnlyDictionary<string, object>);")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteSerializeMethod(
            CodeWriter writer,
            IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public object Serialize(object value)")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("if(value is null)").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync('{').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("return null;").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync('}').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                // TODO : we need to handle loops
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync($"var input = ({descriptor.Name})value;")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("var map = new Dictionary<string, object>();")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                foreach (IInputFieldDescriptor field in descriptor.Fields)
                {
                    string typeName = field.InputObjectType is null
                        ? field.Type.NamedType().Name.Value
                        : field.InputObjectType.Name;

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync($"map[\"{field.Field.Name}\"] = ")
                        .ConfigureAwait(false);
                    await writer.WriteAsync("Serialize(").ConfigureAwait(false);
                    await writer.WriteAsync($"input.{GetPropertyName(field.Name)}, ")
                        .ConfigureAwait(false);
                    await writer.WriteAsync('_').ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(typeName)).ConfigureAwait(false);
                    await writer.WriteAsync("Serializer);").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("return map;").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public object Serialize(object value, IValueSerializer serializer)")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("if (value is IList list)").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync('{').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("var serializedList = new List<object>();")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("foreach (object element in list)")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('{').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync(
                            "serializedList.Add(Serialize(value, serializer));")
                            .ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('}').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("return serializedList;")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync('}').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "return serializer.Serialize(value);")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteDeserializeMethod(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(
                "public object Deserialize(object value)")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "throw new NotSupportedException(")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "\"Deserializing input values is not supported.\");")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
