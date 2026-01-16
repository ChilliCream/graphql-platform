using System.Collections;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Text.Json;
using static HotChocolate.Execution.ResultFieldNames;

namespace HotChocolate.Execution;

public static class JsonValueFormatter
{
    // TODO : are the options still needed?
    public static void WriteValue(
        JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case JsonDocument doc:
                WriteJsonElement(doc.RootElement, writer, options, nullIgnoreCondition);
                break;

            case JsonElement element:
                WriteJsonElement(element, writer, options, nullIgnoreCondition);
                break;

            case RawJsonValue rawJsonValue:
                writer.WriteRawValue(rawJsonValue.Value.Span);
                break;

            case Dictionary<string, object?> dict:
                WriteDictionary(writer, dict, options, nullIgnoreCondition);
                break;

            case IReadOnlyDictionary<string, object?> dict:
                WriteDictionary(writer, dict, options, nullIgnoreCondition);
                break;

            case IList list:
                WriteList(writer, list, options, nullIgnoreCondition);
                break;

            case IError error:
                WriteError(writer, error, options, nullIgnoreCondition);
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

            case IResultDataJsonFormatter formatter:
                formatter.WriteTo(writer, options, nullIgnoreCondition);
                break;

            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    private static void WriteJsonElement(
        JsonElement element,
        JsonWriter writer,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind is JsonValueKind.Null
                        && (nullIgnoreCondition & JsonNullIgnoreCondition.Fields) == JsonNullIgnoreCondition.Fields)
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteValue(writer, property.Value, options, nullIgnoreCondition);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Null
                        && (nullIgnoreCondition & JsonNullIgnoreCondition.Lists) == JsonNullIgnoreCondition.Lists)
                    {
                        continue;
                    }

                    WriteValue(writer, item, options, nullIgnoreCondition);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
            {
                var value = JsonMarshal.GetRawUtf8Value(element);
                writer.WriteStringValue(value, skipEscaping: true);
                break;
            }

            case JsonValueKind.Number:
            {
                var value = JsonMarshal.GetRawUtf8Value(element);
                writer.WriteNumberValue(value);
                break;
            }

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
                throw new NotSupportedException();
        }
    }

    public static void WriteDictionary(
        JsonWriter writer,
        IReadOnlyDictionary<string, object?> dict,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null
                && (nullIgnoreCondition & JsonNullIgnoreCondition.Fields) == JsonNullIgnoreCondition.Fields)
            {
                continue;
            }

            writer.WritePropertyName(item.Key);
            WriteValue(writer, item.Value, options, nullIgnoreCondition);
        }

        writer.WriteEndObject();
    }

    private static void WriteList(
        JsonWriter writer,
        IList list,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            var element = list[i];

            if (element is null
                && (nullIgnoreCondition & JsonNullIgnoreCondition.Lists) == JsonNullIgnoreCondition.Lists)
            {
                continue;
            }

            WriteValue(writer, element, options, nullIgnoreCondition);
        }

        writer.WriteEndArray();
    }

    public static void WriteErrors(
        JsonWriter writer,
        IReadOnlyList<IError> errors,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        if (errors is { Count: > 0 })
        {
            writer.WritePropertyName(ResultFieldNames.Errors);

            writer.WriteStartArray();

            for (var i = 0; i < errors.Count; i++)
            {
                WriteError(writer, errors[i], options, nullIgnoreCondition);
            }

            writer.WriteEndArray();
        }
    }

    public static void WriteError(
        JsonWriter writer,
        IError error,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(Message);
        writer.WriteStringValue(error.Message);

        WriteLocations(writer, error.Locations);
        WritePath(writer, error.Path);
        WriteExtensions(writer, error.Extensions, options, nullIgnoreCondition);

        writer.WriteEndObject();
    }

    public static void WriteExtensions(
        JsonWriter writer,
        IReadOnlyDictionary<string, object?>? dict,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        if (dict is { Count: > 0 })
        {
            writer.WritePropertyName(Extensions);
            WriteDictionary(writer, dict, options, nullIgnoreCondition);
        }
    }

    public static void WriteIncremental(
        JsonWriter writer,
        OperationResult result,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        if (result.Pending is { Count: > 0 } pending)
        {
            writer.WritePropertyName(Pending);

            writer.WriteStartArray();

            for (var i = 0; i < pending.Count; i++)
            {
                WriteIncrementalPendingItem(writer, pending[i]);
            }

            writer.WriteEndArray();
        }

        if (result.Incremental is { Count: > 0 } incremental)
        {
            writer.WritePropertyName(Incremental);

            writer.WriteStartArray();

            for (var i = 0; i < incremental.Count; i++)
            {
                WriteIncrementalItem(writer, incremental[i], options, nullIgnoreCondition);
            }

            writer.WriteEndArray();
        }

        if (result.Completed is { Count: > 0 } completed)
        {
            writer.WritePropertyName(Completed);

            writer.WriteStartArray();

            for (var i = 0; i < completed.Count; i++)
            {
                WriteIncrementalCompletedItem(writer, completed[i]);
            }

            writer.WriteEndArray();
        }

        if (result.HasNext.HasValue)
        {
            writer.WritePropertyName(HasNext);
            writer.WriteBooleanValue(result.HasNext.Value);
        }
    }

    private static void WriteIncrementalPendingItem(JsonWriter writer, PendingResult item)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(Id);
        writer.WriteNumberValue(item.Id);

        writer.WritePropertyName(ResultFieldNames.Path);
        WritePathValue(writer, item.Path);

        if (!string.IsNullOrEmpty(item.Label))
        {
            writer.WritePropertyName(Label);
            writer.WriteStringValue(item.Label);
        }

        writer.WriteEndObject();
    }

    private static void WriteIncrementalItem(
        JsonWriter writer,
        IIncrementalResult item,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(Id);
        writer.WriteNumberValue(item.Id);

        if (item.Errors is { Count: > 0 })
        {
            WriteErrors(writer, item.Errors, options, nullIgnoreCondition);
        }

        if (item is IIncrementalObjectResult objectResult)
        {
            writer.WritePropertyName(Data);

            // TODO: Write actual data
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
        else if (item is IIncrementalListResult listResult)
        {
            writer.WritePropertyName(Items);

            // TODO: Write actual data
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
        else
        {
            throw new NotSupportedException();
        }

        writer.WriteEndObject();
    }

    private static void WriteIncrementalCompletedItem(JsonWriter writer, CompletedResult item)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(Id);
        writer.WriteNumberValue(item.Id);

        writer.WriteEndObject();
    }

    private static void WriteLocations(JsonWriter writer, IReadOnlyList<Location>? locations)
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

    private static void WriteLocation(JsonWriter writer, Location location)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(Line);
        writer.WriteNumberValue(location.Line);
        writer.WritePropertyName(Column);
        writer.WriteNumberValue(location.Column);
        writer.WriteEndObject();
    }

    private static void WritePath(JsonWriter writer, Path? path)
    {
        if (path is not null)
        {
            writer.WritePropertyName(ResultFieldNames.Path);
            WritePathValue(writer, path);
        }
    }

    private static void WritePathValue(JsonWriter writer, Path path)
    {
        if (path.IsRoot)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();

        foreach (var segment in path.EnumerateSegments())
        {
            switch (segment)
            {
                case NamePathSegment n:
                    writer.WriteStringValue(n.Name);
                    break;

                case IndexerPathSegment n:
                    writer.WriteNumberValue(n.Index);
                    break;
            }
        }

        writer.WriteEndArray();
    }
}
