using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ClassGenerator
    {
        public async Task WriteAsync(
            CodeWriter writer,
            ClassDescriptor classDescriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(classDescriptor.Name);
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

            for (int i = 0; i < classDescriptor.Implements.Count; i++)
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
                await writer.WriteAsync(classDescriptor.Implements[i].Name);
                await writer.WriteLineAsync();
            }

            writer.DecreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

            for (int i = 0; i < classDescriptor.Implements.Count; i++)
            {
                if (classDescriptor.Implements[i].Type is IComplexOutputType complexType)
                {
                    foreach (FieldDescriptor fieldDescriptor in classDescriptor.Implements[i].Fields)
                    {
                        string typeName = typeLookup.GetTypeName(
                            fieldDescriptor.Selection,
                            fieldDescriptor.Type,
                            false);

                        string propertyName = GetPropertyName(fieldDescriptor.ResponseName);
                        bool isListType = fieldDescriptor.Type.IsListType();

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
                            typeName = typeLookup.GetTypeName(
                                fieldDescriptor.Selection,
                                fieldDescriptor.Type,
                                true);

                            await writer.WriteIndentAsync();
                            await writer.WriteAsync(typeName);
                            await writer.WriteSpaceAsync();
                            await writer.WriteAsync(classDescriptor.Implements[i].Name);
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
