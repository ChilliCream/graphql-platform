using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace StrawberryShake.Json
{
    public class JsonOperationRequestSerializer
    {
        public void Serialize(OperationRequest request, IBufferWriter<byte> bufferWriter)
        {
            using var jsonWriter = new Utf8JsonWriter(bufferWriter);

            Serialize(request, jsonWriter);
        }

        public void Serialize(OperationRequest request, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();

            WriteRequest(request, jsonWriter);
            WriteVariables(request, jsonWriter);
            WriteExtensions(request, jsonWriter);

            jsonWriter.WriteEndObject();
        }

        private static void WriteRequest(OperationRequest request, Utf8JsonWriter writer)
        {
            if (request.Id is not null)
            {
                writer.WriteString("id", request.Id);
            }

            if (request.Document.Body.Length > 0)
            {
                writer.WriteString("query", request.Document.Body);
            }

            writer.WriteString("operationName", request.Name);
        }

        private static void WriteVariables(OperationRequest request, Utf8JsonWriter writer)
        {
            if (request.Variables.Count > 0)
            {
                writer.WritePropertyName("variables");
                WriteDictionary(request.Variables, writer);
            }
        }

        private static void WriteExtensions(OperationRequest request, Utf8JsonWriter writer)
        {
            if (request.GetExtensionsOrNull() is { Count: > 0 } extensions)
            {
                writer.WritePropertyName("extensions");
                WriteDictionary(extensions, writer);
            }
        }

        private static void WriteValue(object? value, Utf8JsonWriter writer)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            switch (value)
            {
                case IEnumerable<KeyValuePair<string, object?>> dict:
                    WriteDictionary(dict, writer);
                    break;

                case IList list:
                    WriteList(list, writer);
                    break;

                case string s:
                    writer.WriteStringValue(s);
                    break;

                case byte b:
                    writer.WriteNumberValue(b);
                    break;

                case short s:
                    writer.WriteNumberValue(s);
                    break;

                case ushort s:
                    writer.WriteNumberValue(s);
                    break;

                case int i:
                    writer.WriteNumberValue(i);
                    break;

                case uint i:
                    writer.WriteNumberValue(i);
                    break;

                case long l:
                    writer.WriteNumberValue(l);
                    break;

                case ulong l:
                    writer.WriteNumberValue(l);
                    break;

                case float f:
                    writer.WriteNumberValue(f);
                    break;

                case double d:
                    writer.WriteNumberValue(d);
                    break;

                case decimal d:
                    writer.WriteNumberValue(d);
                    break;

                case bool b:
                    writer.WriteBooleanValue(b);
                    break;

                case Uri u:
                    writer.WriteStringValue(u.ToString());
                    break;

                default:
                    writer.WriteStringValue(value.ToString());
                    break;
            }
        }

        private static void WriteDictionary(
            IEnumerable<KeyValuePair<string, object?>> dictionary,
            Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            foreach (KeyValuePair<string, object?> property in dictionary)
            {
                writer.WritePropertyName(property.Key);
                WriteValue(property.Value, writer);
            }

            writer.WriteEndObject();
        }

        private static void WriteList(IList list, Utf8JsonWriter writer)
        {
            writer.WriteStartArray();

            foreach (object? element in list)
            {
                WriteValue(element, writer);
            }

            writer.WriteEndArray();
        }

        public static readonly JsonOperationRequestSerializer Default = new();
    }
}
