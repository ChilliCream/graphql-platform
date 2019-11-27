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
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public class ").ConfigureAwait(false);
            await writer.WriteAsync(GetClassName(descriptor.Name)).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(": IDocument").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await WriteArrayFieldAsync(
                    writer,
                    nameof(descriptor.HashName),
                    descriptor.HashName)
                    .ConfigureAwait(false);
                await WriteArrayFieldAsync(
                    writer,
                    nameof(descriptor.Hash),
                    descriptor.Hash)
                    .ConfigureAwait(false);
                await WriteArrayFieldAsync(
                    writer,
                    "Content",
                    descriptor.Document)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WritePropertyFieldAsync(writer, nameof(descriptor.HashName))
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WritePropertyFieldAsync(writer, nameof(descriptor.Hash))
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WritePropertyFieldAsync(writer, "Content").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteDefaultPropertyFieldAsync(writer, descriptor.Name)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteToStringAsync(writer, descriptor.OriginalDocument)
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static Task WriteArrayFieldAsync(
            CodeWriter writer,
            string name,
            string value) =>
            WriteArrayFieldAsync(writer, name, Encoding.UTF8.GetBytes(value));

        private static async Task WriteArrayFieldAsync(
            CodeWriter writer,
            string name,
            byte[] bytes)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("private readonly byte[] _").ConfigureAwait(false);
            await writer.WriteAsync(GetFieldName(name)).ConfigureAwait(false);
            await writer.WriteAsync(" = new byte[]").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (i > 0)
                    {
                        await writer.WriteAsync(',').ConfigureAwait(false);
                    }
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(bytes[i].ToString()).ConfigureAwait(false);
                }
            }

            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteAsync(';').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static async Task WritePropertyFieldAsync(
            CodeWriter writer,
            string name)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public ReadOnlySpan<byte> ").ConfigureAwait(false);
            await writer.WriteAsync(GetPropertyName(name)).ConfigureAwait(false);
            await writer.WriteAsync(" => _").ConfigureAwait(false);
            await writer.WriteAsync(GetFieldName(name)).ConfigureAwait(false);
            await writer.WriteAsync(';').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static async Task WriteDefaultPropertyFieldAsync(
            CodeWriter writer,
            string name)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public static ").ConfigureAwait(false);
            await writer.WriteAsync(GetClassName(name)).ConfigureAwait(false);
            await writer.WriteAsync(" Default").ConfigureAwait(false);
            await writer.WriteAsync(" { get; } = new ").ConfigureAwait(false);
            await writer.WriteAsync(GetClassName(name)).ConfigureAwait(false);
            await writer.WriteAsync("();").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static async Task WriteToStringAsync(
            CodeWriter writer,
            DocumentNode document)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public override string ToString() => ")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                string documentString =
                    QuerySyntaxSerializer.Serialize(document, true)
                        .Replace("\"", "\"\"")
                        .Replace("\r\n", "\n")
                        .Replace("\n\r", "\n")
                        .Replace("\r", "\n")
                        .Replace("\n", "\n" + writer.GetIndentString());

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("@\"").ConfigureAwait(false);
                await writer.WriteAsync(documentString).ConfigureAwait(false);
                await writer.WriteAsync("\";").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }
    }
}
