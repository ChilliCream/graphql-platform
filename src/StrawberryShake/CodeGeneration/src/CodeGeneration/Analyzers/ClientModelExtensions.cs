using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public static class ClientModelExtensions
    {
        private static readonly JsonWriterOptions _options =
            new JsonWriterOptions { Indented = true };

        public static string ToJson(this ClientModel model)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new Utf8JsonWriter(memoryStream, _options);

            writer.WriteStartObject();

            writer.WritePropertyName("documents");
            SerializeDocuments(writer, model.Documents);

            writer.WritePropertyName("types");
            SerializeTypes(writer, model.Types);

            writer.WriteEndObject();

            writer.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        private static void SerializeDocuments(
            Utf8JsonWriter writer,
            IEnumerable<DocumentModel> documents)
        {
            writer.WriteStartArray();

            foreach (DocumentModel document in documents)
            {
                SerializeDocument(writer, document);
            }

            writer.WriteEndArray();
        }

        private static void SerializeDocument(
            Utf8JsonWriter writer,
            DocumentModel document)
        {
            writer.WriteStartObject();

            writer.WriteString("original",
                QuerySyntaxSerializer.Serialize(document.OriginalDocument));
            writer.WriteString("optimized",
                QuerySyntaxSerializer.Serialize(document.OptimizedDocument));
            writer.WriteString("hashAlgorithm", document.HashAlgorithm);
            writer.WriteString("hash", document.Hash);

            writer.WritePropertyName("operations");

            writer.WriteStartArray();

            foreach (OperationModel operation in document.Operations)
            {
                SerializeOperation(writer, operation);
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void SerializeOperation(
            Utf8JsonWriter writer,
            OperationModel operation)
        {
            writer.WriteStartObject();

            writer.WriteString("name", operation.Name);
            writer.WriteString("type", operation.Type.Name);

            writer.WritePropertyName("arguments");

            writer.WriteStartArray();

            foreach (ArgumentModel argument in operation.Arguments)
            {
                SerializeArgument(writer, argument);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("parser");

            SerializeResultParser(writer, operation.Parser);

            writer.WriteEndObject();
        }

        private static void SerializeArgument(
           Utf8JsonWriter writer,
           ArgumentModel argument)
        {
            writer.WriteStartObject();

            writer.WriteString("name", argument.Name);
            writer.WriteString("type", argument.Type.Print());
            if (argument.DefaultValue is { })
            {
                writer.WriteString("default", QuerySyntaxSerializer.Serialize(argument.DefaultValue));
            }

            writer.WriteEndObject();
        }

        private static void SerializeResultParser(
            Utf8JsonWriter writer,
            ParserModel resultParser)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("returnType");

            SerializeComplexOutputType(writer, resultParser.ReturnType);

            writer.WritePropertyName("fieldParsers");

            writer.WriteStartArray();

            foreach (FieldParserModel fieldParser in resultParser.FieldParsers)
            {
                SerializeFieldParser(writer, fieldParser);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("leafTypes");

            writer.WriteStartArray();

            foreach (INamedType leafType in resultParser.LeafTypes)
            {
                writer.WriteStringValue(leafType.Name);
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void SerializeFieldParser(
            Utf8JsonWriter writer,
            FieldParserModel fieldParser)
        {
            writer.WriteStartObject();

            if (fieldParser.Selection.Alias is { })
            {
                writer.WriteString("alias", fieldParser.Selection.Alias.Value);
            }
            writer.WriteString("name", fieldParser.Selection.Name.Value);
            writer.WriteString("path", fieldParser.Path.ToString());

            writer.WritePropertyName("returnType");

            SerializeComplexOutputType(writer, fieldParser.ReturnType);

            writer.WritePropertyName("possibleTypes");

            writer.WriteStartArray();

            foreach (ComplexOutputTypeModel possibleType in fieldParser.PossibleTypes)
            {
                SerializeComplexOutputType(writer, possibleType);
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void SerializeTypes(Utf8JsonWriter writer, IEnumerable<ITypeModel> types)
        {
            writer.WriteStartArray();

            foreach (ITypeModel type in types)
            {
                switch (type)
                {
                    case ComplexOutputTypeModel outputType:
                        SerializeComplexOutputType(writer, outputType);
                        break;
                    case InputObjectTypeModel inputType:
                        SerializeComplexInputType(writer, inputType);
                        break;
                    case EnumTypeModel enumType:
                        SerializeEnumType(writer, enumType);
                        break;
                }

            }

            writer.WriteEndArray();
        }

        private static void SerializeComplexOutputType(
            Utf8JsonWriter writer,
            ComplexOutputTypeModel outputType)
        {
            writer.WriteStartObject();

            writer.WriteString("type", outputType.IsInterface ? "interface" : "class");
            writer.WriteString("name", outputType.Name);
            writer.WriteString("description", outputType.Description);
            writer.WriteString("typeName", outputType.Type.Name);

            writer.WritePropertyName("implements");

            writer.WriteStartArray();

            foreach (ComplexOutputTypeModel implements in outputType.Types)
            {
                writer.WriteStringValue(implements.Name);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("fields");

            writer.WriteStartArray();

            foreach (OutputFieldModel field in outputType.Fields)
            {
                writer.WriteStartObject();

                writer.WriteString("name", field.Name);
                writer.WriteString("description", field.Description);
                writer.WriteString("fieldName", field.Field.Name);
                writer.WriteString("type", field.Type.Print());
                writer.WriteString("path", field.Path.ToString());

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void SerializeComplexInputType(
            Utf8JsonWriter writer,
            InputObjectTypeModel inputObjectType)
        {
            writer.WriteStartObject();

            writer.WriteString("type", "input");
            writer.WriteString("name", inputObjectType.Name);
            writer.WriteString("description", inputObjectType.Description);
            writer.WriteString("typeName", inputObjectType.Type.Name);

            writer.WritePropertyName("fields");

            writer.WriteStartArray();

            foreach (InputFieldModel field in inputObjectType.Fields)
            {
                writer.WriteStartObject();

                writer.WriteString("name", field.Name);
                writer.WriteString("description", field.Description);
                writer.WriteString("fieldName", field.Field.Name);
                writer.WriteString("type", field.Type.Print());

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void SerializeEnumType(Utf8JsonWriter writer, EnumTypeModel enumType)
        {
            writer.WriteStartObject();

            writer.WriteString("type", "enum");
            writer.WriteString("name", enumType.Name);
            writer.WriteString("description", enumType.Description);
            writer.WriteString("typeName", enumType.Type.Name);
            writer.WriteString("underlyingType", enumType.UnderlyingType);

            writer.WritePropertyName("values");

            writer.WriteStartArray();

            foreach (EnumValueModel value in enumType.Values)
            {
                writer.WriteStartObject();

                writer.WriteString("name", value.Name);
                writer.WriteString("description", value.Description);
                writer.WriteString("underlyingValue", value.UnderlyingValue);
                writer.WriteString("enumValue", value.Value.Name);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
