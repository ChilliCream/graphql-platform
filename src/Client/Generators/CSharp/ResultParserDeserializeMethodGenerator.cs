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
                .Where(t => t.Type.NamedType().IsLeafType())
                .Select(t => t.Type))
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
                        throw new NotImplementedException();
                    }
                    else if (type.IsListType()
                        && type.ListType().ElementType.IsLeafType())
                    {
                        await WriteDeserializeLeafList(
                            writer,
                            typeLookup,
                            methodName,
                            type,
                            type.NamedType().Name,
                            serializerMethod);
                    }
                    else
                    {
                        await WriteDeserializeLeaf(
                            writer,
                            methodName,
                            typeLookup.GetLeafClrTypeName(type),
                            type.NamedType().Name,
                            serializerMethod,
                            type.IsNonNullType());
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
                }).ConfigureAwait(false);
        }

        private async Task WriteDeserializeLeafList(
            CodeWriter writer,
            ITypeLookup typeLookup,
            string methodName,
            IType type,
            string schemaTypeName,
            string serializerMethod)
        {
            IType elementType = type.ElementType();
            string clrTypeName = typeLookup.GetLeafClrTypeName(type);
            string clrElementTypeName = typeLookup.GetLeafClrTypeName(elementType);

            await WriteDeserializerMethodAsync(
                writer, methodName, clrTypeName, async () =>
                {
                    await WriteNullHandlingAsync(
                        writer, "obj", "list", type.IsNonNullType())
                        .ConfigureAwait(false);
                    await WriteDeserializeListAsync(
                        writer, "list", "i", "element",
                        clrElementTypeName, elementType.IsNonNullType(),
                        () => WriteAddElementAsync(
                            writer, clrElementTypeName, schemaTypeName,
                            "list", "i", "element", serializerMethod))
                        .ConfigureAwait(false);
                }).ConfigureAwait(false); ;
        }

        private Task WriteDeserializeNestedLeafList(
            CodeWriter writer,
            string methodName,
            ITypeInfo typeInfo,
            string serializerMethod)
        {
            throw new NotImplementedException();
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
            string listIndex,
            string elementName,
            string clrElementTypeName,
            bool isElementNonNull,
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
                $"for (int {listIndex} = 0; {listIndex} < {listName}Length; {listIndex}++)")
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    $"JsonElement {elementName} = {listName}[i];")
                    .ConfigureAwait(false);

                if (isElementNonNull)
                {
                    using (writer.IncreaseIndent())
                    {
                        await body().ConfigureAwait(false);
                    }
                }
                else
                {
                    await writer.WriteIndentedLineAsync(
                        $"if ({elementName}.ValueKind == JsonValueKind.Null)")
                        .ConfigureAwait(false);
                    await writer.WriteIndentedLineAsync("{")
                        .ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            $"{listName}List[i] = null;")
                            .ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync("}")
                        .ConfigureAwait(false);
                    await writer.WriteIndentedLineAsync("else")
                        .ConfigureAwait(false);
                    await writer.WriteIndentedLineAsync("{")
                        .ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await body().ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync("}")
                        .ConfigureAwait(false);
                }
            }
            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
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
                    $"JsonElement {valueName} = {parameterName}.GetProperty(fieldName);")
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
            string listIndex,
            string elementName,
            string serializerMethod)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync($"{listName}[{listIndex}] = ").ConfigureAwait(false);
            await WriteSerializerAsync(
                writer, clrTypeName, schemaTypeName,
                elementName, serializerMethod)
                .ConfigureAwait(false);
            await writer.WriteAsync(";").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteSerializerAsync(
           CodeWriter writer,
           string clrTypeName,
           string schemaTypeName,
           string valueName,
           string serializerMethod)
        {
            await writer.WriteAsync(
                $"({clrTypeName})_{GetFieldName(schemaTypeName)}Serializer." +
                $"Deserialize({valueName}.{serializerMethod}())")
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
                types.Push(current);
                current = current.InnerType();
            }

            if (types.Count == 0 || !(types.Peek() is NonNullType))
            {
                sb.Append("Nullable");
            }
            sb.Append(type.NamedType().Name);

            return sb.ToString();
        }
    }
}
