using System.Collections.Generic;
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
        private readonly LanguageVersion _languageVersion;

        public InputClassSerializerGenerator(LanguageVersion languageVersion)
        {
            _languageVersion = languageVersion;
        }

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
                await writer.WriteAsync(": IInputSerializer").ConfigureAwait(false);
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

                await WriteProperties(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteInitializationAsync(writer, descriptor).ConfigureAwait(false);
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
            var serializers = new HashSet<string>();

            await writer.WriteIndentedLineAsync(
                "private bool _needsInitialization = true;");

            foreach (IInputFieldDescriptor field in descriptor.Fields)
            {
                string typeName = field.InputObjectType is null
                    ? field.Type.NamedType().Name.Value
                    : field.InputObjectType.Name;

                if (serializers.Add(typeName))
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("private IValueSerializer")
                        .ConfigureAwait(false);

                    if (_languageVersion == LanguageVersion.CSharp_8_0)
                    {
                        await writer.WriteAsync("?")
                            .ConfigureAwait(false);
                    }

                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                    await writer.WriteAsync('_').ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(typeName)).ConfigureAwait(false);
                    await writer.WriteAsync("Serializer").ConfigureAwait(false);
                    await writer.WriteAsync(';').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task WriteInitializationAsync(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            var serializers = new HashSet<string>();

            await writer.WriteIndentedLineAsync(
                "public void Initialize(IValueSerializerResolver serializerResolver)");
            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync("if (serializerResolver is null)");
                await writer.WriteIndentedLineAsync("{");
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "throw new ArgumentNullException(nameof(serializerResolver));");
                }
                await writer.WriteIndentedLineAsync("}");

                for (int i = 0; i < descriptor.Fields.Count; i++)
                {
                    IInputFieldDescriptor field = descriptor.Fields[i];

                    string typeName = field.InputObjectType is null
                        ? field.Type.NamedType().Name.Value
                        : field.InputObjectType.Name;

                    if (serializers.Add(typeName))
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync('_').ConfigureAwait(false);
                        await writer.WriteAsync(GetFieldName(typeName)).ConfigureAwait(false);
                        await writer.WriteAsync("Serializer = ").ConfigureAwait(false);
                        await writer.WriteAsync(
                            $"serializerResolver.GetValueSerializer(\"{typeName}\");")
                            .ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                }
                await writer.WriteIndentedLineAsync("_needsInitialization = false;");
            }

            await writer.WriteIndentedLineAsync("}");
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
            if (_languageVersion == LanguageVersion.CSharp_8_0)
            {
                await writer.WriteAsync("public object? Serialize(object? value)")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteAsync("public object Serialize(object value)")
                    .ConfigureAwait(false);
            }
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "if (_needsInitialization)")
                    .ConfigureAwait(false);
                await writer.WriteIndentedLineAsync("{")
                    .ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "throw new InvalidOperationException(")
                        .ConfigureAwait(false);
                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "$\"The serializer for type `{Name}` has not been initialized.\");")
                            .ConfigureAwait(false);
                        ;
                    }
                }

                await writer.WriteIndentedLineAsync("}")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteNonNullHandling(writer).ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync($"var input = ({descriptor.Name})value;")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                if (_languageVersion == LanguageVersion.CSharp_8_0)
                {
                    await writer.WriteAsync("var map = new Dictionary<string, object?>();")
                        .ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteAsync("var map = new Dictionary<string, object>();")
                        .ConfigureAwait(false);
                }
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                foreach (IInputFieldDescriptor field in descriptor.Fields)
                {
                    string typeName = field.InputObjectType is null
                        ? field.Type.NamedType().Name.Value
                        : field.InputObjectType.Name;

                    IType type = field.Type.IsNonNullType() ? field.Type.InnerType() : field.Type;
                    string serializerName = SerializerNameUtils.CreateSerializerName(type);

                    await writer.WriteIndentedLineAsync(
                        $"if (input.{GetPropertyName(field.Name)}.HasValue)")
                        .ConfigureAwait(false);
                    await writer.WriteIndentedLineAsync("{");

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync($"map.Add(\"{field.Field.Name}\", ")
                            .ConfigureAwait(false);
                        await writer.WriteAsync($"{serializerName}(").ConfigureAwait(false);
                        await writer.WriteAsync($"input.{GetPropertyName(field.Name)}.Value));")
                            .ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync("}");
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

            await WriteTypeSerializerMethodsAsync(writer, descriptor).ConfigureAwait(false);
        }

        private async Task WriteTypeSerializerMethodsAsync(
            CodeWriter writer,
            IInputClassDescriptor descriptor)
        {
            var generatedMethods = new HashSet<string>();

            foreach (IInputFieldDescriptor field in descriptor.Fields)
            {
                await WriteTypeSerializerMethodAsync(
                    writer, field.Type, generatedMethods)
                    .ConfigureAwait(false);
            }
        }

        private async Task<string> WriteTypeSerializerMethodAsync(
            CodeWriter writer,
            IType type,
            ISet<string> generatedMethods)
        {
            IType actualType = type.IsNonNullType() ? type.InnerType() : type;
            string serializerName = SerializerNameUtils.CreateSerializerName(actualType);

            if (!generatedMethods.Add(serializerName))
            {
                return serializerName;
            }

            if (actualType.IsListType())
            {
                IType elementType = type.ElementType();

                string itemSerializer = await WriteTypeSerializerMethodAsync(
                    writer, elementType, generatedMethods)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteTypeSerializerMethodHeaderAsync(writer, serializerName)
                    .ConfigureAwait(false);

                await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    if (!type.IsNonNullType())
                    {
                        await WriteNonNullHandling(writer).ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync(
                        "IList source = (IList)value;")
                        .ConfigureAwait(false);

                    if (_languageVersion == LanguageVersion.CSharp_8_0)
                    {
                        await writer.WriteIndentedLineAsync(
                            "object?[] result = new object?[source.Count];")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteIndentedLineAsync(
                            "object[] result = new object[source.Count];")
                            .ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync(
                        "for(int i = 0; i < source.Count; i++)")
                        .ConfigureAwait(false);
                    await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            $"result[i] = {itemSerializer}(source[i]);")
                            .ConfigureAwait(false);
                    }
                    await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
                    await writer.WriteIndentedLineAsync("return result;").ConfigureAwait(false);
                }
                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
            }
            else
            {
                await WriteTypeSerializerMethodHeaderAsync(writer, serializerName)
                    .ConfigureAwait(false);

                await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    if (!type.IsNonNullType())
                    {
                        await WriteNonNullHandling(writer).ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "return _")
                        .ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(actualType.NamedType().Name))
                        .ConfigureAwait(false);
                    await writer.WriteAsync(
                        _languageVersion == LanguageVersion.CSharp_8_0
                            ? "Serializer!.Serialize(value);"
                            : "Serializer.Serialize(value);")
                            .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
            }

            return serializerName;
        }

        private async Task WriteTypeSerializerMethodHeaderAsync(
            CodeWriter writer,
            string serializerName)
        {
            if (_languageVersion == LanguageVersion.CSharp_8_0)
            {
                await writer.WriteIndentedLineAsync(
                    $"private object? {serializerName}(object? value)")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteIndentedLineAsync(
                    $"private object {serializerName}(object value)")
                    .ConfigureAwait(false);
            }
        }

        private async Task WriteNonNullHandling(CodeWriter writer)
        {
            await writer.WriteIndentedLineAsync("if (value is null)").ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync("return null;").ConfigureAwait(false);
            }
            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync();
        }

        private async Task WriteDeserializeMethod(
           CodeWriter writer,
           IInputClassDescriptor descriptor)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(
                _languageVersion == LanguageVersion.CSharp_8_0
                    ? "public object? Deserialize(object? value)"
                    : "public object Deserialize(object value)")
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
