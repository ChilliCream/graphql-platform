using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Serialization.JsonConstants;

namespace HotChocolate.Execution.Serialization;

public sealed class JsonQueryResultSerializer : IQueryResultSerializer
{
    private readonly JsonWriterOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="JsonQueryResultSerializer" />.
    /// </summary>
    /// <param name="indented">
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without any extra white space.
    /// </param>
    /// <param name="encoder">
    /// Gets or sets the encoder to use when escaping strings, or null to use the default encoder.
    /// </param>
    public JsonQueryResultSerializer(bool indented = false, JavaScriptEncoder? encoder = null)
    {
        _options = new JsonWriterOptions
        {
            Indented = indented, 
            Encoder = encoder ?? JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }

    public unsafe string Serialize(IQueryResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }
        
        using var buffer = new ArrayWriter();

        Serialize(result, buffer);

        fixed (byte* b = buffer.GetInternalBuffer())
        {
            return Encoding.UTF8.GetString(b, buffer.Length);
        }
    }

    /// <inheritdoc />
    public void Serialize(IQueryResult result, IBufferWriter<byte> writer)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        SerializeInternal(result, writer);
    }
    
    private void SerializeInternal(IQueryResult result, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer, _options);
        WriteResult(jsonWriter, result);
        jsonWriter.Flush();
    }

    /// <inheritdoc />
    public Task SerializeAsync(
        IQueryResult result,
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

        return SerializeInternalAsync(result, stream, cancellationToken);
    }
    
    private async Task SerializeInternalAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        await using var writer = new Utf8JsonWriter(outputStream, _options);

        WriteResult(writer, result);
        using var buffer = new ArrayWriter();
        SerializeInternal(result, buffer);

#if NETSTANDARD2_0
        await outputStream
            .WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, cancellationToken)
            .ConfigureAwait(false);
#else
        await outputStream
            .WriteAsync(buffer.Body, cancellationToken)
            .ConfigureAwait(false);
#endif

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteResult(Utf8JsonWriter writer, IQueryResult result)
    {
        writer.WriteStartObject();

        WritePatchInfo(writer, result);
        WriteErrors(writer, result.Errors);
        WriteData(writer, result.Data);
        WriteExtensions(writer, result.Extensions);
        WriteHasNext(writer, result);

        writer.WriteEndObject();
    }

    private static void WritePatchInfo(
        Utf8JsonWriter writer,
        IQueryResult result)
    {
        if (result.Label is not null)
        {
            writer.WriteString("label", result.Label);
        }

        if (result.Path is not null)
        {
            WritePath(writer, result.Path);
        }
    }

    private static void WriteHasNext(
        Utf8JsonWriter writer,
        IQueryResult result)
    {
        if (result.HasNext.HasValue)
        {
            writer.WriteBoolean("hasNext", result.HasNext.Value);
        }
    }

    private void WriteData(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?>? data)
    {
        if (data is { Count: > 0 })
        {
            writer.WritePropertyName(Data);

            if (data is IResultMap resultMap)
            {
                WriteResultMap(writer, resultMap);
            }
            else
            {
                WriteDictionary(writer, data);
            }
        }
    }

    private void WriteErrors(Utf8JsonWriter writer, IReadOnlyList<IError>? errors)
    {
        if (errors is { Count: > 0 })
        {
            writer.WritePropertyName(JsonConstants.Errors);

            writer.WriteStartArray();

            for (var i = 0; i < errors.Count; i++)
            {
                WriteError(writer, errors[i]);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteError(Utf8JsonWriter writer, IError error)
    {
        writer.WriteStartObject();

        writer.WriteString(Message, error.Message);

        WriteLocations(writer, error.Locations);
        WritePath(writer, error.Path);
        WriteExtensions(writer, error.Extensions);

        writer.WriteEndObject();
    }

    private static void WriteLocations(Utf8JsonWriter writer, IReadOnlyList<Location>? locations)
    {
        if (locations is { Count: > 0 })
        {
            writer.WritePropertyName(Locations);

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
        writer.WriteNumber(Line, location.Line);
        writer.WriteNumber(Column, location.Column);
        writer.WriteEndObject();
    }

    private static void WritePath(Utf8JsonWriter writer, Path? path)
    {
        if (path is not null && path is not RootPathSegment)
        {
            writer.WritePropertyName(JsonConstants.Path);
            WritePathValue(writer, path);
        }
    }

    private static void WritePathValue(Utf8JsonWriter writer, Path path)
    {
        if (path is RootPathSegment)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();

        IReadOnlyList<object> list = path.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            switch (list[i])
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

                case short n:
                    writer.WriteNumberValue(n);
                    break;

                case long n:
                    writer.WriteNumberValue(n);
                    break;

                default:
                    writer.WriteStringValue(list[i].ToString());
                    break;
            }
        }

        writer.WriteEndArray();
    }

    private void WriteExtensions(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?>? dict)
    {
        if (dict is { Count: > 0 })
        {
            writer.WritePropertyName(Extensions);
            WriteDictionary(writer, dict);
        }
    }

    private void WriteDictionary(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?> dict)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<string, object?> item in dict)
        {
            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteResultMap(
        Utf8JsonWriter writer,
        IResultMap resultMap)
    {
        writer.WriteStartObject();

        for (var i = 0; i < resultMap.Count; i++)
        {
            ResultValue value = resultMap[i];
            if (value.IsInitialized)
            {
                writer.WritePropertyName(value.Name);
                WriteFieldValue(writer, value.Value);
            }
        }

        writer.WriteEndObject();
    }

    private void WriteList(
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

    private void WriteResultMapList(
        Utf8JsonWriter writer,
        IResultMapList list)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is { } m)
            {
                WriteResultMap(writer, m);
            }
            else
            {
                WriteFieldValue(writer, null);
            }
        }

        writer.WriteEndArray();
    }

    private void WriteFieldValue(
        Utf8JsonWriter writer,
        object? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case IResultMap resultMap:
                WriteResultMap(writer, resultMap);
                break;

            case IResultMapList resultMapList:
                WriteResultMapList(writer, resultMapList);
                break;

            case IReadOnlyDictionary<string, object?> dict:
                WriteDictionary(writer, dict);
                break;

            case IList list:
                WriteList(writer, list);
                break;

            case IError error:
                WriteError(writer, error);
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

            case Path p:
                WritePathValue(writer, p);
                break;

            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
