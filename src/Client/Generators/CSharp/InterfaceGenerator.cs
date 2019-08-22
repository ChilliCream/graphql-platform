using System;
using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class InterfaceGenerator
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
                foreach (FieldDescriptor fieldDescriptor in interfaceDescriptor.Fields)
                {
                    if (complexType.Fields.ContainsField(
                        fieldDescriptor.Selection.Name.Value))
                    {
                        string typeName = typeLookup.GetTypeName(
                            fieldDescriptor.Selection,
                            fieldDescriptor.Type,
                            true);

                        string propertyName = GetPropertyName(fieldDescriptor.ResponseName);

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
