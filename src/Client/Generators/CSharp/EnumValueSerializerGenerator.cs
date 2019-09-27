using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class EnumValueSerializerGenerator
        : CodeGenerator<IEnumDescriptor>
    {
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
                descriptor.Name);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(": IValueSerializer");
            }

            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "public string Name => \"{0}\";",
                    descriptor.Name);
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    "public ValueKind Kind => ValueKind.Enum;");
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    "public Type ClrType => typeof({0});",
                    descriptor.Name);
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    "public Type SerializationType => typeof(string);");
                await writer.WriteLineAsync();

                await WriteSerializeMethodAsync(writer, descriptor, typeLookup);
                await writer.WriteLineAsync();

                await WriteDeserializeMethodAsync(writer, descriptor, typeLookup);
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentedLineAsync("}");
        }

        private async Task WriteSerializeMethodAsync(
            CodeWriter writer,
            IEnumDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentedLineAsync(
                "public object? Serialize(object? value)");
            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync("if(value is null)");
                await writer.WriteIndentedLineAsync("{");
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync("return null;");
                }
                await writer.WriteIndentedLineAsync("}");
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    "var enumValue = ({0})value;",
                    descriptor.Name);
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync("switch(enumValue)");
                await writer.WriteIndentedLineAsync("{");

                using (writer.IncreaseIndent())
                {
                    foreach (IEnumValueDescriptor value in descriptor.Values)
                    {
                        await writer.WriteIndentedLineAsync(
                            "case {0}.{1}:",
                            descriptor.Name,
                            value.Name);
                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                "return \"{0}\";",
                                value.Value);
                        }
                    }

                    await writer.WriteIndentedLineAsync("default:");
                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "throw new NotSupportedException();");
                    }
                }

                await writer.WriteIndentedLineAsync("}");
            }

            await writer.WriteIndentedLineAsync("}");
        }

        private async Task WriteDeserializeMethodAsync(
            CodeWriter writer,
            IEnumDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentedLineAsync(
                "public object? Deserialize(object? value)");
            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync("if(value is null)");
                await writer.WriteIndentedLineAsync("{");
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync("return null;");
                }
                await writer.WriteIndentedLineAsync("}");
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    "var stringValue = (string)value;",
                    descriptor.Name);
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync("switch(stringValue)");
                await writer.WriteIndentedLineAsync("{");

                using (writer.IncreaseIndent())
                {
                    foreach (IEnumValueDescriptor value in descriptor.Values)
                    {
                        await writer.WriteIndentedLineAsync(
                            "case \"{0}\":",
                            value.Value);
                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                "return {0}.{1};",
                                descriptor.Name,
                                value.Name);
                        }
                    }

                    await writer.WriteIndentedLineAsync("default:");
                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "throw new NotSupportedException();");
                    }
                }

                await writer.WriteIndentedLineAsync("}");
            }

            await writer.WriteIndentedLineAsync("}");
        }
    }
}
