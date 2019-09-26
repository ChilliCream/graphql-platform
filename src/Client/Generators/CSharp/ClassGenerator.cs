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

            using (writer.IncreaseIndent())
            {
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
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("{").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            if (descriptor.Fields.Count > 0)
            {
                using (writer.IncreaseIndent())
                {
                    await WriteConstructorAsync(writer, descriptor, typeLookup);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    for (int i = 0; i < descriptor.Fields.Count; i++)
                    {
                        IFieldDescriptor fieldDescriptor = descriptor.Fields[i];

                        string typeName = typeLookup.GetTypeName(
                            fieldDescriptor.Type,
                            fieldDescriptor.Selection,
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
                        await writer.WriteAsync("{ get; }").ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteConstructorAsync(
            CodeWriter writer,
            IClassDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentedLineAsync("public {0}(", descriptor.Name);

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Fields.Count; i++)
                {
                    IFieldDescriptor fieldDescriptor = descriptor.Fields[i];

                    string typeName = typeLookup.GetTypeName(
                        fieldDescriptor.Type,
                        fieldDescriptor.Selection,
                        true);

                    string parameterName = GetFieldName(
                        fieldDescriptor.ResponseName);

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(string.Format(
                        "{0} {1}", typeName, parameterName));

                    if (i < descriptor.Fields.Count - 1)
                    {
                        await writer.WriteAsync(", ");
                    }
                    else
                    {
                        await writer.WriteAsync(")");
                    }

                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Fields.Count; i++)
                {
                    IFieldDescriptor fieldDescriptor = descriptor.Fields[i];

                    string propetyName = GetPropertyName(
                        fieldDescriptor.ResponseName);

                    string parameterName = GetFieldName(
                        fieldDescriptor.ResponseName);

                    await writer.WriteIndentedLineAsync(
                        "{0} = {1};",
                        propetyName,
                        parameterName);
                }
            }

            await writer.WriteIndentedLineAsync("}");
        }
    }
}
