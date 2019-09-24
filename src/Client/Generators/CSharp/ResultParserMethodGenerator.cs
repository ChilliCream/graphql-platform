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
            await writer.WriteIndentAsync();
            await writer.WriteAsync("protected override ");
            await writer.WriteAsync(resultTypeName);
            await writer.WriteSpaceAsync();
            await writer.WriteAsync("ParserData(JsonElement data)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("return ");
                await WriteCreateObjectAsync(
                    writer, descriptor, descriptor.PossibleTypes[0],
                    "data", typeLookup);
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteFieldSelectionSet(
            CodeWriter writer,
            IResultParserMethodDescriptor descriptor,
            ITypeLookup typeLookup,
            string resultTypeName)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("private ");
            await writer.WriteAsync(resultTypeName);
            await writer.WriteSpaceAsync();
            await writer.WriteAsync("Parse");
            await writer.WriteAsync(descriptor.Name);

            await writer.WriteAsync('(');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("JsonElement parent,");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                await writer.WriteAsync("string field)");
                await writer.WriteLineAsync();
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                if (!descriptor.ResultType.IsNonNullType())
                {
                    await WriteNullHandlingAsync(writer);
                }

                if (descriptor.ResultType.NamedType().IsAbstractType()
                    && descriptor.PossibleTypes.Count > 1)
                {
                    await WriteParserForMultipleResultTypes(
                        writer, descriptor, typeLookup);
                }
                else
                {
                    await WriteParserForSingleResultType(
                        writer, descriptor, typeLookup);
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
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
                    typeLookup);
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
                        await writer.WriteIndentAsync();
                        await writer.WriteAsync("return ");
                        await WriteCreateObjectAsync(
                            writer,
                            methodDescriptor,
                            m,
                            "obj",
                            typeLookup);
                    });
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
                elementName);
            await writer.WriteLineAsync();
            await writer.WriteIndentedLineAsync("switch(type)");
            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                foreach (IResultParserTypeDescriptor possibleType in methodDescriptor.PossibleTypes)
                {
                    await writer.WriteIndentedLineAsync(
                        "case \"{0}\":",
                        possibleType.ResultDescriptor.Name);

                    using (writer.IncreaseIndent())
                    {
                        await writeObject(possibleType);
                    }

                    await writer.WriteLineAsync();
                }

                await writer.WriteIndentedLineAsync("default:");
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "throw new UnknownSchemaTypeException(type);");
                }
            }

            await writer.WriteIndentedLineAsync("}");
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
                    typeLookup);
            }
            else
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("return ");
                await WriteCreateObjectAsync(
                    writer,
                    methodDescriptor,
                    possibleType,
                    "obj",
                    typeLookup);
            }

            await writer.WriteLineAsync();
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
                jsonElement);

            await writer.WriteIndentedLineAsync(
                "var {0} = new {1}[{2}];",
                listField,
                resultTypeName,
                lengthField);

            await writer.WriteIndentedLineAsync(
                "for (int {0} = 0; {0} < {1}; {0}++)",
                indexField,
                lengthField);

            await writer.WriteIndentAsync();
            using (writer.WriteBraces())
            {
                await writer.WriteIndentedLineAsync(
                    "JsonElement {0} = {1}[{2}];",
                    elementField,
                    jsonElement,
                    indexField);

                if (elementType.IsListType())
                {
                    await WriteListAsync(
                        writer,
                        methodDescriptor,
                        elementField,
                        "inner" + char.ToUpper(elementField[0]) + elementField.Substring(1),
                        "inner" + char.ToUpper(listField[0]) + listField.Substring(1),
                        typeLookup);
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
                            jsonElement,
                            m => WriteCreateListElementAsync(
                                writer,
                                methodDescriptor,
                                m,
                                jsonElement,
                                listField,
                                indexField,
                                typeLookup));
                    }
                    else
                    {
                        await WriteCreateListElementAsync(
                            writer,
                            methodDescriptor,
                            methodDescriptor.PossibleTypes[0],
                            jsonElement,
                            listField,
                            indexField,
                            typeLookup);
                    }
                }
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            await writer.WriteIndentedLineAsync("return {0};", listField);
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
            await writer.WriteIndentAsync();
            await writer.WriteAsync(string.Format(
                "{0}[{1}] = ",
                listField,
                indexField));
            await WriteCreateObjectAsync(
                writer,
                methodDescriptor,
                possibleType,
                jsonElement,
                typeLookup);
        }

        private async Task WriteCreateObjectAsync(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            IResultParserTypeDescriptor possibleType,
            string jsonElement,
            ITypeLookup typeLookup)
        {
            await writer.WriteAsync("new ");
            await writer.WriteAsync(possibleType.ResultDescriptor.Name);
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('(');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteObjectPropertyDeserializationAsync(
                    writer,
                    possibleType,
                    jsonElement,
                    typeLookup);
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync(')');
            await writer.WriteAsync(';');
            await writer.WriteLineAsync();
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

                await writer.WriteIndentAsync();

                if (fieldDescriptor.Type.NamedType().IsLeafType())
                {
                    ITypeInfo typeInfo = typeLookup.GetTypeInfo(
                        fieldDescriptor.Type,
                        true);

                    string deserializeMethod =
                        ResultParserDeserializeMethodGenerator.CreateDeserializerName(typeInfo);

                    await writer.WriteAsync(deserializeMethod);
                    await writer.WriteAsync('(');
                    await writer.WriteAsync(jsonElement);
                    await writer.WriteAsync(", \"");
                    await writer.WriteAsync(fieldDescriptor.ResponseName);
                    await writer.WriteAsync("\")");
                }
                else
                {
                    await writer.WriteAsync("Parse");
                    await writer.WriteAsync(GetPathName(fieldDescriptor.Path));
                    await writer.WriteAsync('(');
                    await writer.WriteAsync(jsonElement);
                    await writer.WriteAsync(", \"");
                    await writer.WriteAsync(fieldDescriptor.ResponseName);
                    await writer.WriteAsync("\")");
                }

                if (i < possibleType.ResultDescriptor.Fields.Count - 1)
                {
                    await writer.WriteAsync(',');
                }

                await writer.WriteLineAsync();

            }
        }

        private async Task WriteNullHandlingAsync(CodeWriter writer)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("if (!parent.TryGetProperty(field, out JsonElement obj))");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            using (writer.WriteBraces())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("return null;");
            }
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();
        }
    }
}
