using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class EnumValueSerializerGenerator
        : CodeGenerator<IEnumDescriptor>
    {
        private readonly LanguageVersion _languageVersion;

        public EnumValueSerializerGenerator(LanguageVersion languageVersion)
        {
            _languageVersion = languageVersion;
        }

        protected override string CreateFileName(IEnumDescriptor descriptor)
        {
            return descriptor.Name + "ValueSerializer.cs";
        }

        protected override async Task WriteAsync(
            CodeWriter writer,
            IEnumDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentedLineAsync(
                "public class {0}ValueSerializer",
                descriptor.Name).ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(": IValueSerializer").ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "public string Name => \"{0}\";",
                    descriptor.Name)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync(
                    "public ValueKind Kind => ValueKind.Enum;")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync(
                    "public Type ClrType => typeof({0});",
                    descriptor.Name)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync(
                    "public Type SerializationType => typeof(string);")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteSerializeMethodAsync(writer, descriptor, typeLookup)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteDeserializeMethodAsync(writer, descriptor, typeLookup)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private async Task WriteSerializeMethodAsync(
            CodeWriter writer,
            IEnumDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            if (_languageVersion == LanguageVersion.CSharp_8_0)
            {
                await writer.WriteIndentedLineAsync(
                    "public object? Serialize(object? value)")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteIndentedLineAsync(
                    "public object Serialize(object value)")
                    .ConfigureAwait(false);
            }
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync("if (value is null)").ConfigureAwait(false);
                await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync("return null;").ConfigureAwait(false);
                }
                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync(
                    "var enumValue = ({0})value;",
                    descriptor.Name)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync("switch(enumValue)").ConfigureAwait(false);
                await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    foreach (IEnumValueDescriptor value in descriptor.Values)
                    {
                        await writer.WriteIndentedLineAsync(
                            "case {0}.{1}:",
                            descriptor.Name,
                            value.Name)
                            .ConfigureAwait(false);
                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                "return \"{0}\";",
                                value.Value)
                                .ConfigureAwait(false);
                        }
                    }

                    await writer.WriteIndentedLineAsync("default:").ConfigureAwait(false);
                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "throw new NotSupportedException();")
                            .ConfigureAwait(false);
                    }
                }

                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private async Task WriteDeserializeMethodAsync(
            CodeWriter writer,
            IEnumDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            if (_languageVersion == LanguageVersion.CSharp_8_0)
            {
                await writer.WriteIndentedLineAsync(
                    "public object? Deserialize(object? serialized)")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteIndentedLineAsync(
                    "public object Deserialize(object serialized)")
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync("if (serialized is null)")
                    .ConfigureAwait(false);
                await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync("return null;")
                        .ConfigureAwait(false);
                }
                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync(
                    "var stringValue = (string)serialized;",
                    descriptor.Name)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync("switch(stringValue)")
                    .ConfigureAwait(false);
                await writer.WriteIndentedLineAsync("{")
                    .ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    foreach (IEnumValueDescriptor value in descriptor.Values)
                    {
                        await writer.WriteIndentedLineAsync(
                            "case \"{0}\":",
                            value.Value)
                            .ConfigureAwait(false);
                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                "return {0}.{1};",
                                descriptor.Name,
                                value.Name)
                                .ConfigureAwait(false);
                        }
                    }

                    await writer.WriteIndentedLineAsync("default:").ConfigureAwait(false);
                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "throw new NotSupportedException();")
                            .ConfigureAwait(false);
                    }
                }

                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}
