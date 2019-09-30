using System;
using System.Threading;
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
        protected override async Task WriteAsync(
            CodeWriter writer,
            IInterfaceDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public interface ");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < descriptor.Implements.Count; i++)
                {
                    await writer.WriteIndentAsync();

                    if (i == 0)
                    {
                        await writer.WriteAsync(':');
                    }
                    else
                    {
                        await writer.WriteAsync(',');
                    }

                    await writer.WriteSpaceAsync();
                    await writer.WriteAsync(descriptor.Implements[i].Name);
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

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
                                await writer.WriteLineAsync();
                            }

                            await writer.WriteIndentAsync();
                            await writer.WriteAsync(typeName);
                            await writer.WriteSpaceAsync();
                            await writer.WriteAsync(propertyName);
                            await writer.WriteSpaceAsync();
                            await writer.WriteAsync("{ get; }");
                            await writer.WriteLineAsync();
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

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
            await writer.WriteLineAsync();
        }
    }
}
