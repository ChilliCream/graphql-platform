using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace StrawberryShake.Http
{
    public class JsonOperationSerializer
        : IOperationSerializer
    {
        private IReadOnlyDictionary<string, IValueSerializer> _serializers;

        public JsonOperationSerializer(
            IEnumerable<IValueSerializer> serializers)
        {
            if (serializers is null)
            {
                throw new ArgumentNullException(nameof(serializers));
            }

            _serializers = serializers.ToDictionary();
        }

        public Task SerializeAsync(
            IOperation operation,
            IReadOnlyDictionary<string, object> extensions,
            bool includeDocument,
            Stream requestStream)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (requestStream is null)
            {
                throw new ArgumentNullException(nameof(requestStream));
            }

            var writer = new Utf8JsonWriter(requestStream);
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteStringValue(operation.Document.Hash);

            if (includeDocument)
            {
                writer.WritePropertyName("query");
                writer.WriteStringValue(operation.Document.Content);
            }

            writer.WritePropertyName("operationName");
            writer.WriteStringValue(operation.Name);

            IReadOnlyDictionary<string, object> variables =
                SerializeVariables(operation);
            if (variables != null)
            {
                WriteValue(variables, writer);
            }

            if (extensions != null && extensions.Count != 0)
            {
                writer.WritePropertyName("extensions");
                WriteValue(extensions, writer);
            }

            writer.WriteEndObject();

            return writer.FlushAsync();
        }

        private IReadOnlyDictionary<string, object> SerializeVariables(
            IOperation operation)
        {
            IReadOnlyList<VariableValue> variableValues =
                operation.GetVariableValues();

            if (variableValues.Count > 0)
            {
                var map = new Dictionary<string, object>();

                foreach (VariableValue variableValue in variableValues)
                {
                    if (!_serializers.TryGetValue(
                        variableValue.TypeName,
                        out IValueSerializer serializer))
                    {
                        throw new SerializerNotFoundException(
                            variableValue.TypeName);
                    }

                    map.Add(
                        variableValue.Name,
                        SerializeVariable(variableValue.Value, serializer));
                }

                return map;
            }

            return null;
        }

        private object SerializeVariable(
            object obj,
            IValueSerializer serializer)
        {
            if (obj is IList list)
            {
                var serialized = new List<object>();

                foreach (object element in list)
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

            writer.WriteEndObject();
        }
    }
}
