using System;
using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class InterfaceGenerator
        : CodeGenerator<IInterfaceDescriptor>
    {
        public InterfaceGenerator(ClientGeneratorOptions options)
            : base(options)
        {
        }

        protected override async Task WriteAsync(
            CodeWriter writer,
            IInterfaceDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(
                $"{ModelAccessModifier} partial interface ")
                .ConfigureAwait(false);
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
                    await writer.WriteAsync(descriptor.Implements[i].Name)
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("{").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                if (descriptor.Type is IComplexOutputType complexType)
                {
                    for (int i = 0; i < descriptor.Fields.Count; i++)
                    {
                        IFieldDescriptor fieldDescriptor = descriptor.Fields[i];

                        if (complexType.Fields.ContainsField(
                            fieldDescriptor.Selection.Name.Value))
                        {
                            string typeName = typeLookup.GetTypeName(
                                fieldDescriptor.Type,
                                fieldDescriptor.Selection,
                                true);

                            string propertyName = GetPropertyName(fieldDescriptor.ResponseName);

                            if (i > 0)
                            {
                                await writer.WriteLineAsync().ConfigureAwait(false);
                            }

                            await writer.WriteIndentAsync().ConfigureAwait(false);
                            await writer.WriteAsync(typeName).ConfigureAwait(false);
                            await writer.WriteSpaceAsync().ConfigureAwait(false);
                            await writer.WriteAsync(propertyName).ConfigureAwait(false);
                            await writer.WriteSpaceAsync().ConfigureAwait(false);
                            await writer.WriteAsync("{ get; }").ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            // TODO : exception
                            // TODO : resources
                            throw new Exception("Unknown field.");
                        }
                    }
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
