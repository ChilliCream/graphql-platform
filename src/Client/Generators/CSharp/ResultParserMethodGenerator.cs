using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class ResultParserMethodGenerator
    {
        public async Task WriteAsync(
            CodeWriter writer,
            IResultParseMethodDescriptor methodDescriptor,
            ITypeLookup typeLookup)
        {
            string resultTypeName = typeLookup.GetTypeName(
                methodDescriptor.ResultSelection,
                methodDescriptor.ResultType,
                true);

            await writer.WriteIndentAsync();
            await writer.WriteAsync("private static ");
            await writer.WriteAsync(resultTypeName);
            await writer.WriteSpaceAsync();
            await writer.WriteAsync(methodDescriptor.Name);

            await writer.WriteAsync('(');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("JsonElement parent,");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync("ReadOnlySpan<byte> field)");
                await writer.WriteLineAsync();
            }

            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteAsync("if (!parent.TryGetProperty(field, out JsonElement obj))");
                await writer.WriteLineAsync();
                using (writer.WriteBraces())
                {
                    await writer.WriteAsync("return null;");
                }
                await writer.WriteLineAsync();

                // TODO : MULTI CASE
                await writer.WriteAsync("string type = obj.GetProperty(_typename).GetString();");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync();

                await writer.WriteAsync("switch (type)");
                using (writer.WriteBraces())
                {
                    foreach (var x in methodDescriptor.PossibleTypes)
                    {
                        await writer.WriteAsync("case \"Droid\":");
                        await writer.WriteLineAsync();



                    }
                }


            }


            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(methodDescriptor.Name);
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

        }
    }
}
