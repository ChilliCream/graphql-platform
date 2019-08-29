using System.Collections.Generic;
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
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

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

            writer.DecreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

            writer.IncreaseIndent();

            foreach (IFieldDescriptor fieldDescriptor in descriptor.Fields)
            {
                string typeName = typeLookup.GetTypeName(
                    fieldDescriptor.Selection,
                    fieldDescriptor.Type,
                    true);

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
            }

            writer.DecreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
            await writer.WriteLineAsync();
        }
    }
}
