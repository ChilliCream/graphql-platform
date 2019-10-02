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

            foreach (IFieldDescriptor fieldDescriptor in possibleType.ResultDescriptor.Fields)
            {
                if (fieldDescriptor.Type.NamedType().IsLeafType())
                {
                    ITypeInfo typeInfo = typeLookup.GetTypeInfo(
                        fieldDescriptor.Type,
                        false);

                    string methodName = CreateDeserializerName(
                        fieldDescriptor.Type,
                        typeInfo.SchemaTypeName);

                    if (generatedMethods.Add(methodName))
                    {
                        string serializerMethod = _jsonMethod[typeInfo.SerializationType];

                        if (fieldDescriptor.Type.IsListType()
                            && fieldDescriptor.Type.ListType().ElementType.IsListType())
                        {
                            if (!first)
                            {
                                await writer.WriteLineAsync();
                            }
                            first = false;
                            await WriteDeserializeNestedLeafList(
                                writer, methodName, typeInfo, serializerMethod);

                        }
                        else if (fieldDescriptor.Type.IsListType()
                            && fieldDescriptor.Type.ListType().ElementType.IsLeafType())
                        {
                            if (!first)
                            {
                                await writer.WriteLineAsync();
                            }
                            first = false;
                            await WriteDeserializeLeafList(
                                writer, methodName, typeInfo, serializerMethod);
                        }
                        else
                        {
                            if (!first)
                            {
                                await writer.WriteLineAsync();
                            }
                            first = false;
                            await WriteDeserializeLeaf(
                                writer, methodName, typeInfo, serializerMethod);
                        }
                    }
                }
            }
        }

        private async Task WriteDeserializeLeaf(
            CodeWriter writer,
            string methodName,
            ITypeInfo typeInfo,
            string serializerMethod)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync(
                $"private {typeInfo.ClrTypeName} {methodName}(JsonElement obj, string fieldName)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteTryGetFieldAsync(writer, "obj", "value");
                await writer.WriteLineAsync();

                if (typeInfo.IsNullable)
                {
                    await WriteReturnNullIfValueIsNull(writer, "value");
                    await writer.WriteLineAsync();
                }

                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    $"return ({typeInfo.ClrTypeName})_{GetFieldName(typeInfo.SchemaTypeName)}" +
                    $"Serializer.Serialize(value.{serializerMethod}());");
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
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

                await WriteReturnNullIfValueIsNull(writer, "value");
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

                await WriteReturnNullIfValueIsNull(writer, "value");
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

        private async Task WriteReturnNullIfValueIsNull(
            CodeWriter writer,
            string elementName)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync(
                $"if ({elementName}.ValueKind == JsonValueKind.Null)");
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

        private async Task WriteAddElementAsync(
            CodeWriter writer,
            string listName,
            string elementName,
            string clrTypeName,
            string schemaTypeName,
            string serializerMethod)
        {
            await writer.WriteIndentedLineAsync(
                $"{listName}.Add((${clrTypeName})_${schemaTypeName}Serializer" +
                $".Serialize({elementName}.{serializerMethod}());")
                .ConfigureAwait(false);
        }

        internal static string CreateDeserializerName(IType type, string typeName)
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

            sb.Append(typeName);

            return sb.ToString();
        }
    }
}
