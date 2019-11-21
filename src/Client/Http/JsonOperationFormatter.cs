using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http
{
    public class JsonOperationFormatter
        : IOperationFormatter
    {
        private IValueSerializerResolver _valueSerializerResolver;

        public JsonOperationFormatter(
            IValueSerializerResolver valueSerializerResolver)
        {
            if (valueSerializerResolver is null)
            {
                throw new ArgumentNullException(nameof(valueSerializerResolver));
            }

            _valueSerializerResolver = valueSerializerResolver;
        }

        public Task SerializeAsync(
            IOperation operation,
            Stream stream,
            OperationFormatterOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            OperationFormatterOptions opt = options ?? OperationFormatterOptions.Default;

            return SerializeInternalAsync(
                operation, stream, opt.Extensions, opt.IncludeId,
                opt.IncludeDocument, cancellationToken);
        }

        private async Task SerializeInternalAsync(
            IOperation operation,
            Stream stream,
            IReadOnlyDictionary<string, object?>? extensions,
            bool includeId,
            bool includeDocument,
            CancellationToken cancellationToken = default)
        {
            await using var jsonWriter = new Utf8JsonWriter(stream);
            WriteJsonRequest(
                operation, jsonWriter, extensions, includeId,
                includeDocument, cancellationToken);
        }

        public void Serialize(
            IOperation operation,
            IBufferWriter<byte> writer,
            OperationFormatterOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var opt = options ?? OperationFormatterOptions.Default;

            SerializeInternal(
                operation, writer, opt.Extensions, opt.IncludeId,
                opt.IncludeDocument, cancellationToken);
        }

        private void SerializeInternal(
            IOperation operation,
            IBufferWriter<byte> writer,
            IReadOnlyDictionary<string, object?>? extensions,
            bool includeId,
            bool includeDocument,
            CancellationToken cancellationToken = default)
        {
            using var jsonWriter = new Utf8JsonWriter(writer);
            WriteJsonRequest(
                operation, jsonWriter, extensions, includeId,
                includeDocument, cancellationToken);
        }

        private void WriteJsonRequest(
            IOperation operation,
            Utf8JsonWriter writer,
            IReadOnlyDictionary<string, object?>? extensions,
            bool includeId,
            bool includeDocument,
            CancellationToken cancellationToken)
        {
            writer.WriteStartObject();

            if (includeId)
            {
                writer.WritePropertyName("id");
                writer.WriteStringValue(operation.Document.Hash);
            }

            if (includeDocument)
            {
                writer.WritePropertyName("query");
                writer.WriteStringValue(operation.Document.Content);
            }

            writer.WritePropertyName("operationName");
            writer.WriteStringValue(operation.Name);

            IReadOnlyDictionary<string, object?>? variables =
                SerializeVariables(operation);
            if (variables != null)
            {
                writer.WritePropertyName("variables");
                WriteValue(variables, writer);
            }

            if (extensions != null && extensions.Count != 0)
            {
                writer.WritePropertyName("extensions");
                WriteValue(extensions, writer);
            }

            writer.WriteEndObject();
        }

        private IReadOnlyDictionary<string, object?>? SerializeVariables(
            IOperation operation)
        {
            IReadOnlyList<VariableValue> variableValues =
                operation.GetVariableValues();

            if (variableValues.Count > 0)
            {
                var map = new Dictionary<string, object?>();

                foreach (VariableValue variableValue in variableValues)
                {
                    IValueSerializer serializer =
                        _valueSerializerResolver.GetValueSerializer(variableValue.TypeName);

                    map.Add(
                        variableValue.Name,
                        SerializeVariable(variableValue.Value, serializer));
                }

                return map;
            }

            return null;
        }

        private object? SerializeVariable(
            object? obj,
            IValueSerializer serializer)
        {
            if (obj is IList list)
            {
                var serialized = new List<object?>();

                foreach (object? element in list)
                {
                    serialized.Add(SerializeVariable(element, serializer));
                }

                return serializer;
            }

            return serializer.Serialize(obj);
        }

        private static void WriteValue(
            object value,
            Utf8JsonWriter writer)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                switch (value)
                {
                    case IReadOnlyDictionary<string, object> obj:
                        writer.WriteStartObject();

                        foreach (KeyValuePair<string, object> variable in obj)
                        {
                            writer.WritePropertyName(variable.Key);
                            WriteValue(variable.Value, writer);
                        }

                        writer.WriteEndObject();
                        break;

                    case IReadOnlyList<object> list:
                        writer.WriteStartArray();

                        foreach (object item in list)
                        {
                            WriteValue(item, writer);
                        }

                        writer.WriteEndArray();
                        break;

                    case byte b:
                        writer.WriteNumberValue(b);
                        break;

                    case short s:
                        writer.WriteNumberValue(s);
                        break;

                    case int i:
                        writer.WriteNumberValue(i);
                        break;

                    case long l:
                        writer.WriteNumberValue(l);
                        break;

                    case ushort us:
                        writer.WriteNumberValue(us);
                        break;

                    case uint ui:
                        writer.WriteNumberValue(ui);
                        break;

                    case ulong ul:
                        writer.WriteNumberValue(ul);
                        break;

                    case float f:
                        writer.WriteNumberValue(f);
                        break;

                    case double d:
                        writer.WriteNumberValue(d);
                        break;

                    case decimal dec:
                        writer.WriteNumberValue(dec);
                        break;

                    case bool b:
                        writer.WriteBooleanValue(b);
                        break;

                    case string s:
                        writer.WriteStringValue(s);
                        break;
                }
            }
        }
    }
}
