
using System;
using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using IInputFieldDescriptor = StrawberryShake.Generators.Descriptors.IInputFieldDescriptor;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class InputClassGenerator
        : ICodeGenerator<IInputClassDescriptor>
    {
        public async Task WriteAsync(
            CodeWriter writer,
            IInputClassDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteLeftBraceAsync();
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                foreach (IInputFieldDescriptor fieldDescriptor in descriptor.Fields)
                {
                    string typeName = typeLookup.GetTypeName(
                        fieldDescriptor.Type,
                        fieldDescriptor.InputObjectType?.Name,
                        false);

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("public ");
                    await writer.WriteAsync(typeName);
                    await writer.WriteSpaceAsync();
                    await writer.WriteAsync(GetPropertyName(fieldDescriptor.Name));
                    await writer.WriteSpaceAsync();
                    await writer.WriteAsync("{ get; set; }");

                    if (fieldDescriptor.Type.IsListType())
                    {
                        await writer.WriteSpaceAsync();
                        await writer.WriteAsync(" = new ");
                        await writer.WriteAsync(typeName);
                        await writer.WriteAsync("();");
                    }

                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync();
                }

                await writer.WriteRightBraceAsync();
                await writer.WriteLineAsync();
            }

            await writer.WriteRightBraceAsync();
            await writer.WriteLineAsync();
        }
    }
}
