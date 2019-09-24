using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class EnumGenerator
        : CodeGenerator<IEnumDescriptor>
    {
        protected override async Task WriteAsync(
            CodeWriter writer,
            IEnumDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentedLineAsync("public enum {0}", descriptor.Name);
            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Values.Count; i++)
                {
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(descriptor.Values[i].Name);

                    if (i == descriptor.Values.Count - 1)
                    {
                        await writer.WriteAsync(",");
                    }

                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteIndentedLineAsync("}");
        }
    }
}
