using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    public sealed class JsonQueryResultSerializer
        : IQueryResultSerializer
    {
        private const string _contentType = "application/json; charset=utf-8";
        private const string _data = "data";
        private const string _errors = "errors";
        private const string _extensions = "extensions";
        private const string _message = "message";
        private const string _locations = "locations";
        private const string _path = "path";
        private const string _line = "line";
        private const string _column = "column";

        private readonly JsonWriterOptions _options;

        public JsonQueryResultSerializer(bool indented = false)
        {
            _options = new JsonWriterOptions { Indented = indented };
        }

        public string ContentType => _contentType;

        public unsafe string Serialize(IReadOnlyQueryResult result)
        {
            using var buffer = new ArrayWriter();

            using var writer = new Utf8JsonWriter(buffer);
            WriteResult(writer, result);
            writer.Flush();

            fixed (byte* b = buffer.GetInternalBuffer())
            {
                return Encoding.UTF8.GetString(b, buffer.Length);
            }
        }

        public async ValueTask SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            await using var writer = new Utf8JsonWriter(stream, _options);

            WriteResult(writer, result);

            await writer.FlushAsync(cancellationToken);
        }

        private static void WriteResult(Utf8JsonWriter writer, IReadOnlyQueryResult result)
        {
            writer.WriteStartObject();

            WriteErrors(writer, result.Errors);
            WriteData(writer, result.Data);
            WriteExtensions(writer, result.Extensions);

            writer.WriteEndObject();
        }

        private static void WriteData(
            Utf8JsonWriter writer,
            IReadOnlyDictionary<string, object> data)
        {
            if (data is { } && data.Count > 0)
            {
                writer.WritePropertyName(_data);
                WriteDictionary(writer, data);
            }
        }

        private static void WriteErrors(Utf8JsonWriter writer, IReadOnlyCollection<IError> errors)
        {
            if (errors is { } && errors.Count > 0)
            {
                writer.WritePropertyName(_errors);

                writer.WriteStartArray();

                foreach(IError error in errors)
                {
                    WriteError(writer, error);
                }

                writer.WriteEndArray();
            }
        }

        private static void WriteError(Utf8JsonWriter writer, IError error)
        {
            writer.WriteStartObject();

            writer.WriteString(_message, error.Message);

            WriteLocations(writer, error.Locations);
            WritePath(writer, error.Path);
            WriteExtensions(writer, error.Extensions);

            writer.WriteEndObject();
        }

        private static void WriteLocations(Utf8JsonWriter writer, IReadOnlyList<Location> locations)
        {
            if (locations is { } && locations.Count > 0)
            {
                writer.WritePropertyName(_locations);

                writer.WriteStartArray();

                for (var i = 0; i < locations.Count; i++)
                {
                    WriteLocation(writer, locations[i]);
                }

                writer.WriteEndArray();
            }
        }

        private static void WriteLocation(Utf8JsonWriter writer, Location location)
        {
            writer.WriteStartObject();
            writer.WriteNumber(_line, location.Line);
            writer.WriteNumber(_column, location.Column);
            writer.WriteEndObject();
        }

        private static void WritePath(Utf8JsonWriter writer, IReadOnlyList<object> path)
        {
            if (path is { } && path.Count > 0)
            {
                writer.WritePropertyName(_path);

                writer.WriteStartArray();

                for (var i = 0; i < path.Count; i++)
                {
                    switch (path[i])
                    {
                        case NameString n:
                            writer.WriteStringValue(n.Value);
                            break;

                        case string s:
                            writer.WriteStringValue(s);
                            break;

                        case int n:
                            writer.WriteNumberValue(n);
                            break;

                        default:
                            throw new InvalidOperationException(
                                "The specified value is of an invalid type " +
                                "for the error path.");
                    }
                }

                writer.WriteEndArray();
            }
        }

        private static void WriteExtensions(
            Utf8JsonWriter writer,
            IReadOnlyDictionary<string, object> dict)
        {
            if (dict is { } && dict.Count > 0)
            {
                writer.WritePropertyName(_extensions);
                WriteDictionary(writer, dict);
            }
        }

        private static void WriteDictionary(
            Utf8JsonWriter writer,
            IReadOnlyDictionary<string, object> dict)
        {
            writer.WriteStartObject();

            foreach (KeyValuePair<string, object> item in dict)
            {
                writer.WritePropertyName(item.Key);
                WriteFieldValue(writer, item.Value);
            }

            writer.WriteEndObject();
        }

        private static void WriteList(
            Utf8JsonWriter writer,
            IList list)
        {
            writer.WriteStartArray();

            for (var i = 0; i < list.Count; i++)
            {
                WriteFieldValue(writer, list[i]);
            }

            writer.WriteEndArray();
        }

        private static void WriteFieldValue(Utf8JsonWriter writer,  object value)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            switch (value)
            {
                case IReadOnlyDictionary<string, object> dict:
                    WriteDictionary(writer, dict);
                    break;

                case IList list:
                    WriteList(writer, list);
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

                case NameString n:
                    writer.WriteStringValue(n.Value);
                    break;

                case Uri u:
                    writer.WriteStringValue(u.ToString());
                    break;

                default:
                    writer.WriteStringValue(value.ToString());
                    break;
            }
        }
    }
}
