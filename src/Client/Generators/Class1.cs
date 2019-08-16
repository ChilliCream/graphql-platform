using System.Runtime.InteropServices.WindowsRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Clients.Generators
{
    public class InterfaceGenerator
    {
        public async Task WriteAsync(
            CodeWriter writer,
            ISchema schema,
            INamedType type,
            SelectionSetNode selectionSet,
            IEnumerable<FieldNode> fields,
            NameString targetName,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public interface ");
            await writer.WriteAsync(targetName.Value);
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
                        await writer.WriteIndentAsync();
                        await writer.WriteAsync(typeLookup.GetTypeName(selectionSet, fieldSelection));
                        await writer.WriteSpaceAsync();
                        await writer.WriteAsync(fieldSelection.Alias.Value);
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

            writer.IncreaseIndent();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
        }
    }

    public interface ITypeLookup
    {
        string GetTypeName(SelectionSetNode selectionSet, FieldNode field);
    }

    public class CodeWriter
        : TextWriter
    {
        public CodeWriter()
        {
        }

        public override Encoding Encoding { get; } = Encoding.UTF8;

        public void WriteIndent() => throw new NotImplementedException();
        public Task WriteIndentAsync() => throw new NotImplementedException();

        public void WriteSpace() => throw new NotImplementedException();
        public Task WriteSpaceAsync() => throw new NotImplementedException();

        public void IncreaseIndent() => throw new NotImplementedException();
        public void DecreaseIndent() => throw new NotImplementedException();
    }
}
