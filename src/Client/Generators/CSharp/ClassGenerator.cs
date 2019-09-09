using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ClassGenerator
        : CodeGenerator<IClassDescriptor>
    {
        protected override async Task WriteAsync(
            CodeWriter writer,
            IClassDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public class ").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            writer.IncreaseIndent();

            for (int i = 0; i < descriptor.Implements.Count; i++)
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);

                if (i == 0)
                {
                    await writer.WriteAsync(':').ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteAsync(',').ConfigureAwait(false);
                }

                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync(descriptor.Implements[i].Name).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            writer.DecreaseIndent();

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("{").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            writer.IncreaseIndent();

            for (int i = 0; i < descriptor.Fields.Count; i++)
            {
                IFieldDescriptor fieldDescriptor = descriptor.Fields[i];

                string typeName = typeLookup.GetTypeName(
                    fieldDescriptor.Selection,
                    fieldDescriptor.Type,
                    true);

                string propertyName = GetPropertyName(fieldDescriptor.ResponseName);
                bool isListType = fieldDescriptor.Type.IsListType();

                if (i > 0)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("public").ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync(typeName).ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync(propertyName).ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync("{ get; set; }").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            writer.DecreaseIndent();

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
