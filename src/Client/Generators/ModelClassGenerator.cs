using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class ModelClassGenerator
    {
        public async Task WriteAsync(
            CodeWriter writer,
            ISchema schema,
            INamedType type,
            string targetName,
            IReadOnlyList<InterfaceCodeDescriptor> implements)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(targetName);
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

            for (int i = 0; i < implements.Count; i++)
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
                await writer.WriteAsync(implements[i].Name);
                await writer.WriteLineAsync();
            }

            writer.DecreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

            for (int i = 0; i < implements.Count; i++)
            {
                if (implements[i].Type is IComplexOutputType complexType)
                {
                    foreach (FieldNode fieldSelection in implements[i].Fields)
                    {
                        if (complexType.Fields.TryGetField(
                            fieldSelection.Name.Value,
                            out IOutputField field))
                        {
                            NameNode name = fieldSelection.Alias ?? fieldSelection.Name;
                            string typeName = "string"; // typeLookup.GetTypeName(selectionSet, fieldSelection);
                            string propertyName = NameUtils.GetPropertyName(name.Value);
                            bool isListType = field.Type.IsListType();

                            await writer.WriteIndentAsync();
                            await writer.WriteAsync("public");
                            await writer.WriteSpaceAsync();
                            await writer.WriteAsync(typeName);
                            await writer.WriteSpaceAsync();
                            await writer.WriteAsync(propertyName);
                            await writer.WriteSpaceAsync();
                            await writer.WriteAsync("{ get; set; }");
                            await writer.WriteLineAsync();

                            if (isListType)
                            {
                                await writer.WriteIndentAsync();
                                await writer.WriteAsync(typeName);
                                await writer.WriteSpaceAsync();
                                await writer.WriteAsync(implements[i].Name);
                                await writer.WriteAsync('.');
                                await writer.WriteAsync(propertyName);
                                await writer.WriteSpaceAsync();
                                await writer.WriteAsync(" => ");
                                await writer.WriteAsync(propertyName);
                                await writer.WriteAsync(';');
                                await writer.WriteLineAsync();
                            }
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



            writer.DecreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
        }
    }
}
