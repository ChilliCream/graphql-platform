using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ResultParserMethodGenerator
        : CodeGenerator<IResultParserMethodDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            IResultParserMethodDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            bool isOperation = descriptor.ResultSelection.Directives.Any(t =>
                t.Name.Value.EqualsOrdinal(GeneratorDirectives.Operation));

            string resultTypeName = isOperation
                ? descriptor.ResultDescriptor.Name
                : typeLookup.GetTypeName(
                    descriptor.ResultType,
                    descriptor.ResultSelection,
                    true);

            if (isOperation)
            {
                return WriteParseDataAsync(
                    writer, descriptor, typeLookup, resultTypeName);
            }

            return WriteFieldSelectionSet(
                writer, descriptor, typeLookup, resultTypeName);
        }

        private async Task WriteParseDataAsync(
           CodeWriter writer,
           IResultParserMethodDescriptor descriptor,
           ITypeLookup typeLookup,
           string resultTypeName)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("protected override ").ConfigureAwait(false);
            await writer.WriteAsync(resultTypeName).ConfigureAwait(false);
            await writer.WriteSpaceAsync().ConfigureAwait(false);
            await writer.WriteAsync("ParserData(JsonElement data)").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("return ").ConfigureAwait(false);
                await WriteCreateObjectAsync(
                    writer, descriptor, descriptor.PossibleTypes[0],
                    "data", typeLookup).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteFieldSelectionSet(
            CodeWriter writer,
            IResultParserMethodDescriptor descriptor,
            ITypeLookup typeLookup,
            string resultTypeName)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("private ").ConfigureAwait(false);
            await writer.WriteAsync(resultTypeName).ConfigureAwait(false);
            await writer.WriteSpaceAsync().ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);

            await writer.WriteAsync('(').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("JsonElement parent,").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("string field)").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await WriteNullHandlingAsync(
                    writer,
                    descriptor.ResultType.IsNonNullType())
                    .ConfigureAwait(false);

                if (descriptor.ResultType.NamedType().IsAbstractType()
                    && descriptor.PossibleTypes.Count > 1)
                {
                    await WriteParserForMultipleResultTypes(
                        writer, descriptor, typeLookup)
                        .ConfigureAwait(false);
                }
                else
                {
                    await WriteParserForSingleResultType(
                        writer, descriptor, typeLookup)
                        .ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteParserForMultipleResultTypes(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            ITypeLookup typeLookup)
        {
            if (methodDescriptor.ResultType.IsListType())
            {
                await WriteListAsync(
                    writer,
                    methodDescriptor,
                    "obj",
                    "element",
                    "list",
                    typeLookup)
                    .ConfigureAwait(false);
            }
            else
            {
                await WriteAbstractTypeHandlingAsync(
                    writer,
                    methodDescriptor,
                    typeLookup,
                    "obj",
                    async m =>
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync("return ").ConfigureAwait(false);
                        await WriteCreateObjectAsync(
                            writer,
                            methodDescriptor,
                            m,
                            "obj",
                            typeLookup)
                            .ConfigureAwait(false);
                    }).ConfigureAwait(false);
            }
        }

        private async Task WriteAbstractTypeHandlingAsync(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            ITypeLookup typeLookup,
            string elementName,
            Func<IResultParserTypeDescriptor, Task> writeObject)
        {
            await writer.WriteIndentedLineAsync(
                "string type = {0}.GetProperty(TypeName).GetString();",
                elementName)
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("switch(type)").ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                foreach (IResultParserTypeDescriptor possibleType in methodDescriptor.PossibleTypes)
                {
                    await writer.WriteIndentedLineAsync(
                        "case \"{0}\":",
                        possibleType.ResultDescriptor.Type.Name)
                        .ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writeObject(possibleType).ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await writer.WriteIndentedLineAsync("default:").ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "throw new UnknownSchemaTypeException(type);")
                        .ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private Task WriteParserForSingleResultType(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            ITypeLookup typeLookup) =>
            WriteParserForSingleResultType(
                writer,
                methodDescriptor,
                methodDescriptor.PossibleTypes[0],
                typeLookup);

        private async Task WriteParserForSingleResultType(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            IResultParserTypeDescriptor possibleType,
            ITypeLookup typeLookup)
        {

            if (methodDescriptor.ResultType.IsListType())
            {
                await WriteListAsync(
                    writer,
                    methodDescriptor,
                    "obj",
                    "element",
                    "list",
                    typeLookup)
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("return ").ConfigureAwait(false);
                await WriteCreateObjectAsync(
                    writer,
                    methodDescriptor,
                    possibleType,
                    "obj",
                    typeLookup)
                    .ConfigureAwait(false);
            }
        }

        private async Task WriteListAsync(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            string jsonElement,
            string elementField,
            string listField,
            ITypeLookup typeLookup)
        {
            IType elementType = methodDescriptor.ResultType.ElementType();

            string resultTypeName = typeLookup.GetTypeName(
                elementType.IsNonNullType() ? elementType : new NonNullType(elementType),
                methodDescriptor.ResultSelection,
                true);

            string lengthField = jsonElement + "Length";
            string indexField = jsonElement + "Index";

            await writer.WriteIndentedLineAsync(
                "int {0} = {1}.GetArrayLength();",
                lengthField,
                jsonElement)
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync(
                "var {0} = new {1}[{2}];",
                listField,
                resultTypeName,
                lengthField)
                .ConfigureAwait(false);

            await writer.WriteIndentedLineAsync(
                "for (int {0} = 0; {0} < {1}; {0}++)",
                indexField,
                lengthField)
                .ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            using (writer.WriteBraces())
            {
                await writer.WriteIndentedLineAsync(
                    "JsonElement {0} = {1}[{2}];",
                    elementField,
                    jsonElement,
                    indexField)
                    .ConfigureAwait(false);

                if (elementType.IsListType())
                {
                    await WriteListAsync(
                        writer,
                        methodDescriptor,
                        elementField,
                        "inner" + char.ToUpper(elementField[0]) + elementField.Substring(1),
                        "inner" + char.ToUpper(listField[0]) + listField.Substring(1),
                        typeLookup)
                        .ConfigureAwait(false);
                }
                else
                {
                    if (elementType.IsAbstractType()
                        && methodDescriptor.PossibleTypes.Count > 1)
                    {
                        await WriteAbstractTypeHandlingAsync(
                            writer,
                            methodDescriptor,
                            typeLookup,
                            elementField,
                            async m =>
                            {
                                await WriteCreateListElementAsync(
                                    writer,
                                    methodDescriptor,
                                    m,
                                    elementField,
                                    listField,
                                    indexField,
                                    typeLookup)
                                    .ConfigureAwait(false);
                                await writer.WriteIndentedLineAsync("break;").ConfigureAwait(false);
                            });
                    }
                    else
                    {
                        await WriteCreateListElementAsync(
                            writer,
                            methodDescriptor,
                            methodDescriptor.PossibleTypes[0],
                            elementField,
                            listField,
                            indexField,
                            typeLookup)
                            .ConfigureAwait(false);
                    }
                }
            }

            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentedLineAsync("return {0};", listField).ConfigureAwait(false);
        }

        private async Task WriteCreateListElementAsync(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            IResultParserTypeDescriptor possibleType,
            string jsonElement,
            string listField,
            string indexField,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(string.Format(
                "{0}[{1}] = ",
                listField,
                indexField))
                .ConfigureAwait(false);
            await WriteCreateObjectAsync(
                writer,
                methodDescriptor,
                possibleType,
                jsonElement,
                typeLookup)
                .ConfigureAwait(false);
        }

        private async Task WriteCreateObjectAsync(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            IResultParserTypeDescriptor possibleType,
            string jsonElement,
            ITypeLookup typeLookup)
        {
            await writer.WriteAsync("new ").ConfigureAwait(false);
            await writer.WriteAsync(possibleType.ResultDescriptor.Name).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('(').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await WriteObjectPropertyDeserializationAsync(
                    writer,
                    possibleType,
                    jsonElement,
                    typeLookup)
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(')').ConfigureAwait(false);
            await writer.WriteAsync(';').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task WriteObjectPropertyDeserializationAsync(
            CodeWriter writer,
            IResultParserTypeDescriptor possibleType,
            string jsonElement,
            ITypeLookup typeLookup)
        {
            for (int i = 0; i < possibleType.ResultDescriptor.Fields.Count; i++)
            {
                IFieldDescriptor fieldDescriptor = possibleType.ResultDescriptor.Fields[i];

                await writer.WriteIndentAsync().ConfigureAwait(false);

                if (fieldDescriptor.Type.NamedType().IsLeafType())
                {
                    ITypeInfo typeInfo = typeLookup.GetTypeInfo(
                        fieldDescriptor.Type,
                        true);

                    string deserializeMethod =
                        SerializerNameUtils.CreateDeserializerName(
                            fieldDescriptor.Type);

                    await writer.WriteAsync(deserializeMethod).ConfigureAwait(false);
                    await writer.WriteAsync('(').ConfigureAwait(false);
                    await writer.WriteAsync(jsonElement).ConfigureAwait(false);
                    await writer.WriteAsync(", \"").ConfigureAwait(false);
                    await writer.WriteAsync(fieldDescriptor.ResponseName).ConfigureAwait(false);
                    await writer.WriteAsync("\")").ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteAsync("Parse").ConfigureAwait(false);
                    await writer.WriteAsync(GetPathName(fieldDescriptor.Path))
                        .ConfigureAwait(false);
                    await writer.WriteAsync('(').ConfigureAwait(false);
                    await writer.WriteAsync(jsonElement).ConfigureAwait(false);
                    await writer.WriteAsync(", \"").ConfigureAwait(false);
                    await writer.WriteAsync(fieldDescriptor.ResponseName).ConfigureAwait(false);
                    await writer.WriteAsync("\")").ConfigureAwait(false);
                }

                if (i < possibleType.ResultDescriptor.Fields.Count - 1)
                {
                    await writer.WriteAsync(',').ConfigureAwait(false);
                }

                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteNullHandlingAsync(CodeWriter writer, bool isNonNullType)
        {
            if (isNonNullType)
            {
                await writer.WriteIndentedLineAsync(
                    "JsonElement obj = parent.GetProperty(field);")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
            else
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "if (!parent.TryGetProperty(field, out JsonElement obj))")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                using (writer.WriteBraces())
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("return null;").ConfigureAwait(false);
                }
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "if (obj.ValueKind == JsonValueKind.Null)")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await writer.WriteIndentAsync().ConfigureAwait(false);
                using (writer.WriteBraces())
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync("return null;").ConfigureAwait(false);
                }
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }
    }
}
