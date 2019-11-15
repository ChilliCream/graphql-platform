using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using IInputFieldDescriptor = StrawberryShake.Generators.Descriptors.IInputFieldDescriptor;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class InputClassGenerator
        : CodeGenerator<IInputClassDescriptor>
    {
        protected override async Task WriteAsync(
            CodeWriter writer,
            IInputClassDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public class ").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteLeftBraceAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Fields.Count; i++)
                {
                    IInputFieldDescriptor fieldDescriptor = descriptor.Fields[i];

                    string typeName = typeLookup.GetTypeName(
                        fieldDescriptor.Type,
                        fieldDescriptor.InputObjectType?.Name,
                        false);

                    if (i > 0)
                    {
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync($"public Optional<{typeName}>")
                        .ConfigureAwait(false);
                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                    await writer.WriteAsync(GetPropertyName(fieldDescriptor.Name))
                        .ConfigureAwait(false);
                    await writer.WriteSpaceAsync().ConfigureAwait(false);
                    await writer.WriteAsync("{ get; set; }").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteRightBraceAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
