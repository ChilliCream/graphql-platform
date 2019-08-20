using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class ModelClassGenerator
    {
        public async Task WriteAsync(
            CodeWriter writer,
            string targetName,
            IReadOnlyList<InterfaceDescriptor> implements,
            ITypeLookup typeLookup)
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
                    foreach (FieldInfo fieldInfo in implements[i].Fields)
                    {
                        string typeName = typeLookup.GetTypeName(fieldInfo.Selection, fieldInfo.Type, false);
                        string propertyName = NameUtils.GetPropertyName(fieldInfo.ResponseName);
                        bool isListType = fieldInfo.Type.IsListType();

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
                            typeName = typeLookup.GetTypeName(fieldInfo.Selection, fieldInfo.Type, true);

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
                }
            }



            writer.DecreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
        }
    }
}
