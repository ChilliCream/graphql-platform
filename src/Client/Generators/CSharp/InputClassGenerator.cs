using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using IInputFieldDescriptor = StrawberryShake.Generators.Descriptors.IInputFieldDescriptor;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class InputClassGenerator
        : CodeGenerator<IInputClassDescriptor>
    {
        protected override async Task WriteAsync(
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
                for (int i = 0; i < descriptor.Fields.Count; i++)
                {
                    IInputFieldDescriptor fieldDescriptor = descriptor.Fields[i];

                    string typeName = typeLookup.GetTypeName(
                        fieldDescriptor.Type,
                        fieldDescriptor.InputObjectType?.Name,
                        false);

                    if (i > 0)
                    {
                        await writer.WriteLineAsync();
                    }

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
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteRightBraceAsync();
            await writer.WriteLineAsync();
        }
    }
}
