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
            InterfaceDescriptor interfaceDescriptor,
            ITypeLookup typeLookup)
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
                foreach (FieldInfo fieldInfo in interfaceDescriptor.Fields)
                {
                    if (complexType.Fields.ContainsField(
                        fieldInfo.Selection.Name.Value))
                    {
                        string typeName = typeLookup.GetTypeName(fieldInfo.Selection, fieldInfo.Type, true);
                        string propertyName = NameUtils.GetPropertyName(fieldInfo.ResponseName);

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
