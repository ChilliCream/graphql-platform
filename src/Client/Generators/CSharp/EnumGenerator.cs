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
            await writer.WriteIndentedLineAsync(
                "public enum {0}", descriptor.Name)
                .ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{")
                .ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Values.Count; i++)
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(descriptor.Values[i].Name).ConfigureAwait(false);

                    if (i < descriptor.Values.Count - 1)
                    {
                        await writer.WriteAsync(",").ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}
