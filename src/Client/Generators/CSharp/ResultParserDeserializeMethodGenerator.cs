using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ResultParserDeserializeMethodGenerator
        : CodeGenerator<IResultParserDescriptor>
    {
        private static readonly Dictionary<Type, string> _jsonMethod =
            new Dictionary<Type, string>
            {
                { typeof(string), "GetString" },
                { typeof(bool), "GetBoolean" },
                { typeof(byte), "GetByte" },
                { typeof(short), "GetInt16" },
                { typeof(int), "GetInt32" },
                { typeof(long), "GetInt64" },
                { typeof(ushort), "GetUInt16" },
                { typeof(uint), "GetUInt32" },
                { typeof(ulong), "GetUInt64" },
                { typeof(decimal), "GetDecimal" },
                { typeof(float), "GetSingle" },
                { typeof(double), "GetDouble" },
                { typeof(bool?), "GetBoolean" },
                { typeof(byte?), "GetByte" },
                { typeof(short?), "GetInt16" },
                { typeof(int?), "GetInt32" },
                { typeof(long?), "GetInt64" },
                { typeof(ushort?), "GetUInt16" },
                { typeof(uint?), "GetUInt32" },
                { typeof(ulong?), "GetUInt64" },
                { typeof(decimal?), "GetDecimal" },
                { typeof(float?), "GetSingle" },
                { typeof(double?), "GetDouble" }
            };

        private LanguageVersion _languageVersion;

        public ResultParserDeserializeMethodGenerator(LanguageVersion languageVersion)
        {
            _languageVersion = languageVersion;
        }

        protected override async Task WriteAsync(
            CodeWriter writer,
            IResultParserDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            var generatedMethods = new HashSet<string>();

            foreach (IResultParserTypeDescriptor possibleType in
                descriptor.ParseMethods.SelectMany(t => t.PossibleTypes))
            {
                await WriteDeserializeMethodAsync(
                    writer, possibleType, typeLookup, generatedMethods);
            }
        }

        private async Task WriteDeserializeMethodAsync(
            CodeWriter writer,
            IResultParserTypeDescriptor possibleType,
            ITypeLookup typeLookup,
            ISet<string> generatedMethods)
        {
            bool first = true;

            foreach (IType type in possibleType.ResultDescriptor.Fields
                .Where(t => t.Type.IsLeafType()).Select(t => t.Type))
            {
                string methodName = CreateDeserializerName(type);

                if (generatedMethods.Add(methodName))
                {
                    Type serializationType = typeLookup.GetSerializationType(type);
                    string serializerMethod = _jsonMethod[serializationType];

                    if (!first)
                    {
                        await writer.WriteLineAsync();
                    }
                    first = false;

                    if (type.IsListType() && type.ListType().ElementType.IsListType())
                    {
                        await WriteDeserializeNestedLeafList(
                            writer, methodName, typeInfo, serializerMethod);

                    }
                    else if (fieldDescriptor.Type.IsListType()
                        && fieldDescriptor.Type.ListType().ElementType.IsLeafType())
                    {
                        await WriteDeserializeLeafList(
                            writer, methodName, typeInfo, serializerMethod);
                    }
                    else
                    {
                        await WriteDeserializeLeaf(
                            writer, methodName, typeInfo, serializerMethod);
                    }
                }
            }
        }

        private async Task WriteDeserializeLeaf(
            CodeWriter writer,
            string methodName,
            string clrTypeName,
            string schemaTypeName,
            string serializerMethod,
            bool isNonNullType)
        {
            await WriteDeserializerMethodAsync(
                writer, methodName, clrTypeName, async () =>
                {
                    await WriteNullHandlingAsync(
                        writer, "obj", "value", isNonNullType)
                        .ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("return ").ConfigureAwait(false);
                    await WriteSerializerAsync(
                        writer, clrTypeName, schemaTypeName,
                        "value", serializerMethod);
                    await writer.WriteAsync(';').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                });
        }

        private async Task WriteDeserializeLeafList(
            CodeWriter writer,
            string methodName,
            ITypeInfo typeInfo,
            string serializerMethod)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync(
                $"private {typeInfo.ClrTypeName} {methodName}" +
                "(JsonElement obj, string fieldName)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteTryGetFieldAsync(writer, "obj", "value");
                await writer.WriteLineAsync();

                await WriteResolveValueAsync()(writer, "value");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "int arrayLength = value.GetArrayLength();");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "var list = new List<string>(arrayLength);");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "for (int i = 0; i < arrayLength; i++)");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync('{');
                await writer.WriteLineAsync();

                using (writer.IncreaseIndent())
                {
                    if (typeInfo.Type.ListType().ElementType.IsNonNullType())
                    {
                        await WriteAddElementAsync(
                            writer, "list", "value[i]", typeInfo.ClrTypeName,
                            typeInfo.SchemaTypeName, serializerMethod);
                    }
                    else
                    {
                        await WriteAddNullableElementAsync(
                            writer, "list", "value[i]",
                            w => WriteAddElementAsync(
                                w, "list", "value[i]", typeInfo.ClrTypeName,
                                typeInfo.SchemaTypeName, serializerMethod));
                    }
                }

                await writer.WriteIndentAsync();
                await writer.WriteAsync('}');
                await writer.WriteLineAsync();
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync("return list;");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteDeserializeNestedLeafList(
            CodeWriter writer,
            string methodName,
            ITypeInfo typeInfo,
            string serializerMethod)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync(
                $"private {typeInfo.ClrTypeName} {methodName}" +
                "(JsonElement obj, string fieldName)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteTryGetFieldAsync(writer, "obj", "value");
                await writer.WriteLineAsync();

                await WriteResolveValueAsync()(writer, "value");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "int arrayLength = value.GetArrayLength();");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "var list = new List<string>(arrayLength);");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "for (int i = 0; i < arrayLength; i++)");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync('{');
                await writer.WriteLineAsync();

                using (writer.IncreaseIndent())
                {
                    if (typeInfo.Type.ListType().ElementType.IsNonNullType())
                    {

                    }
                    else
                    {

                    }
                }

                await writer.WriteIndentAsync();
                await writer.WriteAsync('}');
                await writer.WriteLineAsync();
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync("return list;");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteTryGetFieldAsync(
            CodeWriter writer,
            string objectName,
            string elementName)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync(
                $"if (!{objectName}.TryGetProperty(fieldName, " +
                $"out JsonElement {elementName}))");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("return null;");
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }



        private async Task WriteAddNullableElementAsync(
            CodeWriter writer,
            string listName,
            string elementName,
            Func<CodeWriter, Task> writeAddValue)
        {
            await writer.WriteIndentedLineAsync(
                $"if ({elementName}.ValueKind == JsonValueKind.Null)")
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    $"{listName}.Add(null);")
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("else").ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writeAddValue(writer);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private async Task WriteDeserializerMethodAsync(
            CodeWriter writer,
            string methodName,
            string clrTypeName,
            Func<Task> body)
        {
            await writer.WriteIndentedLineAsync(
                $"private {clrTypeName} {methodName}" +
                "(JsonElement obj, string fieldName)")
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await body().ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private async Task WriteDeserializeListAsync(
            CodeWriter writer,
            string listName,
            string listIndexer,
            string elementName,
            string clrElementTypeName,
            Func<Task> body)
        {
            await writer.WriteIndentedLineAsync(
                $"int {listName}Length = {listName}.GetArrayLength();")
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync(
                $"var {listName}List = new {clrElementTypeName}[{listName}Length];")
                .ConfigureAwait(false);

            await writer.WriteLineAsync();

            await writer.WriteIndentedLineAsync(
                $"for (int {listIndexer} = 0; {listIndexer} < {listName}Length; {listIndexer}++)")
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
            await writer.WriteIndentedLineAsync(
                $"JsonElement {elementName} = {listName}[i];"


            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
        }

        private async Task WriteNullHandlingAsync(
            CodeWriter writer,
            string parameterName,
            string valueName,
            bool isNonNullType)
        {
            if (isNonNullType)
            {
                await writer.WriteIndentedLineAsync(
                    $"JsonElement {valueName} = obj.GetProperty(fieldName);")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteIndentedLineAsync(
                    $"if (!{parameterName}.TryGetProperty(fieldName, " +
                    $"out JsonElement {valueName}))");

                await writer.WriteIndentedLineAsync("{");

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("return null;");
                    await writer.WriteLineAsync();
                }

                await writer.WriteIndentedLineAsync("}");
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    $"if ({valueName}.ValueKind == JsonValueKind.Null)");

                await writer.WriteIndentedLineAsync("{");

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("return null;");
                    await writer.WriteLineAsync();
                }

                await writer.WriteIndentedLineAsync("}");
                await writer.WriteLineAsync();
            }
        }

        private async Task WriteAddElementAsync(
            CodeWriter writer,
            string clrTypeName,
            string schemaTypeName,
            string listName,
            string elementName,
            string serializerMethod)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync($"{listName}.Add(").ConfigureAwait(false);
            await WriteSerializerAsync(
                writer, clrTypeName, schemaTypeName,
                elementName, serializerMethod)
                .ConfigureAwait(false);
            await writer.WriteAsync(");").ConfigureAwait(false);
        }

        private async Task WriteSerializerAsync(
           CodeWriter writer,
           string clrTypeName,
           string schemaTypeName,
           string valueName,
           string serializerMethod)
        {
            await writer.WriteAsync(
                $"({clrTypeName})_{schemaTypeName}Serializer." +
                $"Serialize({valueName}.{serializerMethod}()")
                .ConfigureAwait(false);

            if (_languageVersion == LanguageVersion.CSharp_8_0)
            {
                await writer.WriteAsync('!').ConfigureAwait(false);
            }
        }

        internal static string CreateDeserializerName(IType type)
        {
            IType current = type;
            var types = new Stack<IType>();

            var sb = new StringBuilder();
            sb.Append("Deserialize");

            while (!(current is INamedType))
            {
                if (current is ListType)
                {
                    if (types.Count == 0 || !(types.Peek() is NonNullType))
                    {
                        sb.Append("Nullable");
                    }
                    sb.Append("ListOf");
                }
                current = current.InnerType();
            }

            sb.Append(type.NamedType().Name);

            return sb.ToString();
        }
    }
}
