using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Serialization.JsonConstants;

namespace HotChocolate.Execution.Serialization;

public sealed class JsonQueryResultFormatter : IQueryResultFormatter
{
    private readonly JsonWriterOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="JsonQueryResultFormatter" />.
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
    public JsonQueryResultFormatter(bool indented = false, JavaScriptEncoder? encoder = null)
    {
        _options = new JsonWriterOptions { Indented = indented, Encoder = encoder };
    }

    public unsafe string Format(IQueryResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        using var buffer = new ArrayWriter();

        Format(result, buffer);

        fixed (byte* b = buffer.GetInternalBuffer())
        {
            return Encoding.UTF8.GetString(b, buffer.Length);
        }
    }

    public void Format(IQueryResult result, Utf8JsonWriter writer)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        WriteResult(writer, result);
    }

    public void Format(IError error, Utf8JsonWriter writer)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        WriteError(writer, error);
    }

    public void Format(IReadOnlyList<IError> errors, Utf8JsonWriter writer)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.WriteStartArray();

        for (var i = 0; i < errors.Count; i++)
        {
            WriteError(writer, errors[i]);
        }

        writer.WriteEndArray();
    }

    /// <inheritdoc />
    public void Format(IQueryResult result, IBufferWriter<byte> writer)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        using var jsonWriter = new Utf8JsonWriter(writer, _options);
        WriteResult(jsonWriter, result);
        jsonWriter.Flush();
    }

    /// <inheritdoc />
    public async Task FormatAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (outputStream is null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        await using var writer = new Utf8JsonWriter(outputStream, _options);

        WriteResult(writer, result);

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
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
        if (data is not null)
        {
            writer.WritePropertyName(Data);

            if (data is ObjectResult resultMap)
            {
                WriteObjectResult(writer, resultMap);
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
        if (path is not null)
        {
            writer.WritePropertyName(JsonConstants.Path);
            WritePathValue(writer, path);
        }
    }

    private static void WritePathValue(Utf8JsonWriter writer, Path path)
    {
        if (path.IsRoot)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();

        var list = path.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            switch (list[i])
            {
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

        foreach (var item in dict)
        {
            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteDictionary(
        Utf8JsonWriter writer,
        Dictionary<string, object?> dict)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteObjectResult(
        Utf8JsonWriter writer,
        ObjectResult objectResult)
    {
        writer.WriteStartObject();

        ref var searchSpace = ref objectResult.GetReference();

        for (var i = 0; i < objectResult.Capacity; i++)
        {
            var field = Unsafe.Add(ref searchSpace, i);

            if (field.IsInitialized)
            {
                writer.WritePropertyName(field.Name);
                WriteFieldValue(writer, field.Value);
            }
        }

        writer.WriteEndObject();
    }

    private void WriteListResult(
        Utf8JsonWriter writer,
        ListResult list)
    {
        writer.WriteStartArray();

        ref var searchSpace = ref list.GetReference();

        for (var i = 0; i < list.Count; i++)
        {
            var element = Unsafe.Add(ref searchSpace, i);
            WriteFieldValue(writer, element);
        }

        writer.WriteEndArray();
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

#if NET5_0_OR_GREATER
    private void WriteJsonElement(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteJsonObject(writer, element);
                break;

            case JsonValueKind.Array:
                WriteJsonArray(writer, element);
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WriteJsonObject(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        writer.WriteStartObject();

        foreach (var item in element.EnumerateObject())
        {
            writer.WritePropertyName(item.Name);
            WriteJsonElement(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteJsonArray(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        writer.WriteStartArray();

        foreach (var item in element.EnumerateArray())
        {
            WriteJsonElement(writer, item);
        }

        writer.WriteEndArray();
    }

#endif
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
            case ObjectResult resultMap:
                WriteObjectResult(writer, resultMap);
                break;

            case ListResult resultMapList:
                WriteListResult(writer, resultMapList);
                break;

#if NET5_0_OR_GREATER
            case JsonElement element:
                WriteJsonElement(writer, element);
                break;

            case RawJsonValue rawJsonValue:
                writer.WriteRawValue(rawJsonValue.Value.Span, true);
                break;
#endif
            case Dictionary<string, object?> dict:
                WriteDictionary(writer, dict);
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
