using System.Text;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class QueryGenerator
        : CodeGenerator<IQueryDescriptor>
    {
        protected override async Task WriteAsync(
            CodeWriter writer,
            IQueryDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(GetClassName(descriptor.Name));
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync(": IDocument");
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteArrayField(
                    writer,
                    nameof(descriptor.HashName),
                    descriptor.HashName);
                await WriteArrayField(
                    writer,
                    nameof(descriptor.Hash),
                    descriptor.Hash);
                await WriteArrayField(
                    writer,
                    "Content",
                    descriptor.Document);
                await writer.WriteLineAsync();

                await WritePropertyFieldAsync(writer, nameof(descriptor.HashName));
                await writer.WriteLineAsync();

                await WritePropertyFieldAsync(writer, nameof(descriptor.Hash));
                await writer.WriteLineAsync();

                await WritePropertyFieldAsync(writer, "Content");
                await writer.WriteLineAsync();

                await WriteDefaultPropertyFieldAsync(writer, descriptor.Name);
                await writer.WriteLineAsync();

                await WriteToStringAsync(writer, descriptor.OriginalDocument);
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private Task WriteArrayField(
            CodeWriter writer,
            string name,
            string value) =>
            WriteArrayField(writer, name, Encoding.UTF8.GetBytes(value));

        private async Task WriteArrayField(
            CodeWriter writer,
            string name,
            byte[] bytes)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("private readonly byte[] _");
            await writer.WriteAsync(GetFieldName(name));
            await writer.WriteAsync(" = new byte[]");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (i > 0)
                    {
                        await writer.WriteAsync(',');
                    }
                    await writer.WriteLineAsync();

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(bytes[i].ToString());
                }
            }

            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteAsync(';');
            await writer.WriteLineAsync();
        }

        private async Task WritePropertyFieldAsync(
            CodeWriter writer,
            string name)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ReadOnlySpan<byte> ");
            await writer.WriteAsync(GetPropertyName(name));
            await writer.WriteAsync(" => _");
            await writer.WriteAsync(GetFieldName(name));
            await writer.WriteAsync(';');
            await writer.WriteLineAsync();
        }

        private async Task WriteDefaultPropertyFieldAsync(
            CodeWriter writer,
            string name)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ");
            await writer.WriteAsync(GetClassName(name));
            await writer.WriteAsync(" Default");
            await writer.WriteAsync(" { get; } = new ");
            await writer.WriteAsync(GetClassName(name));
            await writer.WriteAsync("();");
            await writer.WriteLineAsync();
        }

        private async Task WriteToStringAsync(
            CodeWriter writer,
            DocumentNode document)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public override string ToString() => ");
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                string documentString =
                    QuerySyntaxSerializer.Serialize(document, true)
                        .Replace("\"", "\"\"")
                        .Replace("\r\n", "\n")
                        .Replace("\n\r", "\n")
                        .Replace("\r", "\n")
                        .Replace("\n", "\n" + writer.GetIndentString());

                await writer.WriteIndentAsync();
                await writer.WriteAsync("@\"");
                await writer.WriteAsync(documentString);
                await writer.WriteAsync("\";");
                await writer.WriteLineAsync();
            }
        }
    }
}
