using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class ModelInterfaceGenerator
    {
        public async Task WriteAsync(
            CodeWriter writer,
            ISchema schema,
            InterfaceCodeDescriptor interfaceDescriptor)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public interface ");
            await writer.WriteAsync(interfaceDescriptor.Name);
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

            if (interfaceDescriptor.Type is IComplexOutputType complexType)
            {
                foreach (FieldNode fieldSelection in interfaceDescriptor.Fields)
                {
                    if (complexType.Fields.ContainsField(
                        fieldSelection.Name.Value))
                    {
                        NameNode name = fieldSelection.Alias ?? fieldSelection.Name;
                        string typeName = "string"; // typeLookup.GetTypeName(selectionSet, fieldSelection);
                        string propertyName = NameUtils.GetPropertyName(name.Value);

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

            writer.DecreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
        }
    }
}
