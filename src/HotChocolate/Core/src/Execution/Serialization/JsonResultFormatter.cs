using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Serialization.JsonNullIgnoreCondition;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// The default JSON formatter for <see cref="IQueryResult"/>.
/// </summary>
public sealed partial class JsonResultFormatter : IQueryResultFormatter, IExecutionResultFormatter
{
    private readonly JsonWriterOptions _options;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly bool _stripNullProps;
    private readonly bool _stripNullElements;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonResultFormatter"/>.
    /// </summary>
    /// <param name="options">
    /// The JSON result formatter options
    /// </param>
    public JsonResultFormatter(JsonResultFormatterOptions options = default)
    {
        _options = options.CreateWriterOptions();
        _serializerOptions = options.CreateSerializerOptions();
        _stripNullProps = options.NullIgnoreCondition is Fields or All;
        _stripNullElements = options.NullIgnoreCondition is Lists or All;
    }

    /// <inheritdoc cref="IExecutionResultFormatter.FormatAsync"/>
    public async ValueTask FormatAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result.Kind is ExecutionResultKind.SingleResult)
        {
            await FormatAsync((IQueryResult)result, outputStream, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            throw JsonFormatter_ResultNotSupported(nameof(JsonResultFormatter));
        }
    }

    /// <summary>
    /// Formats a query result as JSON string.
    /// </summary>
    /// <param name="result">
    /// The query result.
    /// </param>
    /// <returns>
    /// Returns the JSON string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
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

    /// <summary>
    /// Formats a query result as JSON string.
    /// </summary>
    /// <param name="result">
    /// The query result.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
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

    /// <summary>
    /// Formats a <see cref="IError"/> as JSON string.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void FormatError(IError error, Utf8JsonWriter writer)
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

    /// <summary>
    /// Formats a list of <see cref="IError"/>s as JSON array string.
    /// </summary>
    /// <param name="errors">
    /// The list of error objects.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void FormatErrors(IReadOnlyList<IError> errors, Utf8JsonWriter writer)
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

    /// <inheritdoc cref="IQueryResultFormatter.Format"/>
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

        FormatInternal(result, writer);
    }
    
    private void FormatInternal(IQueryResult result, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer, _options);
        WriteResult(jsonWriter, result);
        jsonWriter.Flush();
    }

    /// <inheritdoc cref="IQueryResultFormatter.FormatAsync"/>
    public ValueTask FormatAsync(
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

        return FormatInternalAsync(result, outputStream, cancellationToken);
    }

    private async ValueTask FormatInternalAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new ArrayWriter();
        FormatInternal(result, buffer);

#if NETSTANDARD2_0
        await outputStream
            .WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, cancellationToken)
            .ConfigureAwait(false);
#else
        await outputStream
            .WriteAsync(buffer.GetWrittenMemory(), cancellationToken)
            .ConfigureAwait(false);
#endif

        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteResult(Utf8JsonWriter writer, IQueryResult result)
    {
        writer.WriteStartObject();

        WriteErrors(writer, result.Errors);
        WriteData(writer, result);
        WriteItems(writer, result.Items);
        WriteIncremental(writer, result.Incremental);
        WriteExtensions(writer, result.Extensions);
        WritePatchInfo(writer, result);
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
        IQueryResult result)
    {
        if (!result.IsDataSet)
        {
            return;
        }

        if (result.Data is null)
        {
            writer.WriteNull(Data);
            return;
        }
        
        writer.WritePropertyName(Data);

        if (result.Data is ObjectResult resultMap)
        {
            WriteObjectResult(writer, resultMap);
        }
        else
        {
            WriteDictionary(writer, result.Data);
        }
    }

    private void WriteItems(Utf8JsonWriter writer, IReadOnlyList<object?>? items)
    {
        if (items is { Count: > 0, })
        {
            writer.WritePropertyName(Items);

            writer.WriteStartArray();

            for (var i = 0; i < items.Count; i++)
            {
                WriteFieldValue(writer, items[i]);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteErrors(Utf8JsonWriter writer, IReadOnlyList<IError>? errors)
    {
        if (errors is { Count: > 0, })
        {
            writer.WritePropertyName(Errors);

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
        if (locations is { Count: > 0, })
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
            writer.WritePropertyName(Path);
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
        if (dict is { Count: > 0, })
        {
            writer.WritePropertyName(Extensions);
            WriteDictionary(writer, dict);
        }
    }

    private void WriteIncremental(Utf8JsonWriter writer, IReadOnlyList<IQueryResult>? patches)
    {
        if (patches is { Count: > 0, })
        {
            writer.WritePropertyName(Incremental);

            writer.WriteStartArray();

            for (var i = 0; i < patches.Count; i++)
            {
                WriteResult(writer, patches[i]);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteDictionary(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?> dict)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null && _stripNullProps)
            {
                continue;
            }

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
            if (item.Value is null && _stripNullProps)
            {
                continue;
            }

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

            if (!field.IsInitialized || (field.Value is null && _stripNullProps))
            {
                continue;
            }

            writer.WritePropertyName(field.Name);
            WriteFieldValue(writer, field.Value);
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

            if (element is null && _stripNullElements)
            {
                continue;
            }

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
            var element = list[i];

            if (element is null && _stripNullElements)
            {
                continue;
            }

            WriteFieldValue(writer, element);
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
            if (item.Value.ValueKind is JsonValueKind.Null && _stripNullProps)
            {
                continue;
            }

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
            if (item.ValueKind is JsonValueKind.Null && _stripNullElements)
            {
                continue;
            }

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

#if NET6_0_OR_GREATER
            case JsonDocument doc:
                WriteJsonElement(writer, doc.RootElement);
                break;

            case JsonElement element:
                WriteJsonElement(writer, element);
                break;

            case RawJsonValue rawJsonValue:
                writer.WriteRawValue(rawJsonValue.Value.Span, true);
                break;
#endif
            case NeedsFormatting unformatted:
                unformatted.FormatValue(writer, _serializerOptions);
                break;

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
