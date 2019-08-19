using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class InterfaceGenerator
    {
        public async Task WriteAsync(
            CodeWriter writer,
            ISchema schema,
            INamedType type,
            SelectionSetNode selectionSet,
            IEnumerable<FieldNode> fields,
            string targetName)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public interface ");
            await writer.WriteAsync(NameUtils.GetInterfaceName(targetName));
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

            if (type is IComplexOutputType complexType)
            {
                foreach (FieldNode fieldSelection in fields)
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
