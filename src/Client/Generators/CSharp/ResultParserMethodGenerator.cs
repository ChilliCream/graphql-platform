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
            string resultTypeName = descriptor.ResultSelection is null
                ? descriptor.ResultDescriptor.Name
                : typeLookup.GetTypeName(
                    descriptor.ResultSelection,
                    descriptor.ResultType,
                    true);

            if (descriptor.ResultSelection is null)
            {
                return WriteOperationSelectionSet(
                    writer, descriptor, typeLookup, resultTypeName);
            }

            return WriteFieldSelectionSet(
                writer, descriptor, typeLookup, resultTypeName);
        }

        private async Task WriteOperationSelectionSet(
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

                if (descriptor.ResultType.NamedType().IsAbstractType())
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
            await writer.WriteIndentAsync();
            await writer.WriteAsync("string type = obj.GetProperty(TypeName).GetString();");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            int last = methodDescriptor.PossibleTypes.Count - 1;

            for (int i = 0; i <= last; i++)
            {
                var possibleType = methodDescriptor.PossibleTypes[i];

                await writer.WriteIndentAsync();
                await writer.WriteAsync("if (string.Equals(type, ");
                await writer.WriteStringValueAsync(possibleType.ResultDescriptor.Name);
                await writer.WriteAsync(", StringComparison.Ordinal))");
                await writer.WriteLineAsync();

                await writer.WriteIndentAsync();
                using (writer.WriteBraces())
                {
                    if (methodDescriptor.ResultType.IsListType())
                    {
                        await WriteListAsync(
                            writer,
                            methodDescriptor,
                            possibleType,
                            "obj",
                            "element",
                            "list",
                            "entity",
                            typeLookup);
                    }
                    else
                    {
                        await WriteCreateObjectAsync(
                            writer,
                            methodDescriptor,
                            possibleType,
                            "obj",
                            typeLookup);
                    }
                }

                await writer.WriteLineAsync();

                if (i < last)
                {
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteLineAsync();

            if (methodDescriptor.UnknownType is null)
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync(
                    "throw new UnknownSchemaTypeException(type);");
                await writer.WriteLineAsync();
            }
            else
            {
                await WriteParserForSingleResultType(
                    writer,
                    methodDescriptor,
                    methodDescriptor.UnknownType,
                    typeLookup);
            }
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
                    possibleType,
                    "obj",
                    "element",
                    "list",
                    "entity",
                    typeLookup);
            }
            else
            {
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
            IResultParserTypeDescriptor possibleType,
            string jsonElement,
            string elementField,
            string listField,
            string entityField,
            ITypeLookup typeLookup)
        {
            IType elementType = methodDescriptor.ResultType.ElementType();

            string resultTypeName = typeLookup.GetTypeName(
                methodDescriptor.ResultSelection,
                elementType,
                true);

            string lengthField = jsonElement + "Length";
            string indexField = jsonElement + "Index";

            await writer.WriteIndentAsync();
            await writer.WriteAsync("int ");
            await writer.WriteAsync(lengthField);
            await writer.WriteAsync(" = ");
            await writer.WriteAsync(jsonElement);
            await writer.WriteAsync(".GetArrayLength();");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("var ");
            await writer.WriteAsync(listField);
            await writer.WriteAsync(" = new ");
            await writer.WriteAsync(resultTypeName);
            await writer.WriteAsync('[');
            await writer.WriteAsync(lengthField);
            await writer.WriteAsync(']');
            await writer.WriteAsync(';');
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("for (int ");
            await writer.WriteAsync(indexField);
            await writer.WriteAsync(" = 0; ");
            await writer.WriteAsync(indexField);
            await writer.WriteAsync($" < {lengthField}; ");
            await writer.WriteAsync(indexField);
            await writer.WriteAsync("++)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            using (writer.WriteBraces())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("JsonElement ");
                await writer.WriteAsync(elementField);
                await writer.WriteAsync(" = ");
                await writer.WriteAsync(jsonElement);
                await writer.WriteAsync('[');
                await writer.WriteAsync(indexField);
                await writer.WriteAsync(']');
                await writer.WriteAsync(';');
                await writer.WriteLineAsync();

                if (elementType.IsListType())
                {
                    await WriteListAsync(
                        writer,
                        methodDescriptor,
                        possibleType,
                        elementField,
                        "inner" + char.ToUpper(elementField[0]) + elementField.Substring(1),
                        "inner" + char.ToUpper(listField[0]) + listField.Substring(1),
                        "inner" + char.ToUpper(entityField[0]) + entityField.Substring(1),
                        typeLookup);
                }
                else
                {
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync("var ");
                    await writer.WriteAsync(entityField);
                    await writer.WriteAsync(" = new ");
                    await writer.WriteAsync(possibleType.ResultDescriptor.Name);
                    await writer.WriteAsync("();");
                    await writer.WriteLineAsync();

                    await WriteObjectPropertyDeserializationAsync(
                        writer,
                        possibleType,
                        elementField,
                        entityField,
                        typeLookup);

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(listField);
                    await writer.WriteAsync('[');
                    await writer.WriteAsync(indexField);
                    await writer.WriteAsync(']');
                    await writer.WriteAsync(" = ");
                    await writer.WriteAsync(entityField);
                    await writer.WriteAsync(';');
                }
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync("return ");
            await writer.WriteAsync(listField);
            await writer.WriteAsync(';');
        }

        private async Task WriteCreateObjectAsync(
            CodeWriter writer,
            IResultParserMethodDescriptor methodDescriptor,
            IResultParserTypeDescriptor possibleType,
            string jsonElement,
            ITypeLookup typeLookup)
        {
            string entityField = GetFieldName(possibleType.ResultDescriptor.Name);

            await writer.WriteIndentAsync();
            await writer.WriteAsync("var ");
            await writer.WriteAsync(entityField);
            await writer.WriteAsync(" = new ");
            await writer.WriteAsync(possibleType.ResultDescriptor.Name);
            await writer.WriteAsync("();");
            await writer.WriteLineAsync();

            await WriteObjectPropertyDeserializationAsync(
                writer,
                possibleType,
                jsonElement,
                entityField,
                typeLookup);

            await writer.WriteIndentAsync();
            await writer.WriteAsync("return ");
            await writer.WriteAsync(entityField);
            await writer.WriteAsync(';');
        }

        private async Task WriteObjectPropertyDeserializationAsync(
           CodeWriter writer,
           IResultParserTypeDescriptor possibleType,
           string jsonElement,
           string entityField,
           ITypeLookup typeLookup)
        {
            foreach (IFieldDescriptor fieldDescriptor in possibleType.ResultDescriptor.Fields)
            {
                await writer.WriteIndentAsync();

                await writer.WriteAsync(entityField);
                await writer.WriteAsync('.');
                await writer.WriteAsync(GetPropertyName(fieldDescriptor.ResponseName));
                await writer.WriteAsync(" = ");

                if (fieldDescriptor.Type.NamedType().IsLeafType())
                {
                    ITypeInfo typeInfo = typeLookup.GetTypeInfo(
                        fieldDescriptor.Selection,
                        fieldDescriptor.Type,
                        true);

                    string deserializeMethod =
                        ResultParserDeserializeMethodGenerator.CreateDeserializerName(typeInfo);

                    await writer.WriteAsync('(');
                    await writer.WriteAsync(typeInfo.ClrTypeName);
                    await writer.WriteAsync(')');
                    await writer.WriteAsync(deserializeMethod);
                    await writer.WriteAsync('(');
                    await writer.WriteAsync(jsonElement);
                    await writer.WriteAsync(", \"");
                    await writer.WriteAsync(fieldDescriptor.ResponseName);
                    await writer.WriteAsync("\");");
                }
                else
                {
                    await writer.WriteAsync("Parse");
                    await writer.WriteAsync(GetPathName(fieldDescriptor.Path));
                    await writer.WriteAsync('(');
                    await writer.WriteAsync(jsonElement);
                    await writer.WriteAsync(", \"");
                    await writer.WriteAsync(fieldDescriptor.ResponseName);
                    await writer.WriteAsync("\");");
                }

                await writer.WriteLineAsync();
            }
        }
    }
}
