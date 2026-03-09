using System.Collections;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Text.Json;
using static HotChocolate.Execution.ResultFieldNames;

namespace HotChocolate.Execution;

public static class JsonValueFormatter
{
    public static void WriteValue(
        JsonWriter writer,
        object? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case JsonDocument doc:
                WriteJsonElement(doc.RootElement, writer, options);
                break;

            case JsonElement element:
                WriteJsonElement(element, writer, options);
                break;

            case RawJsonValue rawJsonValue:
                writer.WriteRawValue(rawJsonValue.Value.Span);
                break;

            case Dictionary<string, object?> dict:
                WriteDictionary(writer, dict, options);
                break;

            case IReadOnlyDictionary<string, object?> dict:
                WriteDictionary(writer, dict, options);
                break;

            case IList list:
                WriteList(writer, list, options);
                break;

            case IError error:
                WriteError(writer, error, options);
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
                formatter.WriteTo(writer, options);
                break;

            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    private static void WriteJsonElement(
        JsonElement element,
        JsonWriter writer,
        JsonSerializerOptions options)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteValue(writer, property.Value, options);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteValue(writer, item, options);
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
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            writer.WritePropertyName(item.Key);
            WriteValue(writer, item.Value, options);
        }

        writer.WriteEndObject();
    }

    private static void WriteList(
        JsonWriter writer,
        IList list,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            WriteValue(writer, list[i], options);
        }

        writer.WriteEndArray();
    }

    public static void WriteErrors(
        JsonWriter writer,
        IReadOnlyList<IError> errors,
        JsonSerializerOptions options)
    {
        if (errors is { Count: > 0 })
        {
            writer.WritePropertyName(ResultFieldNames.Errors);

            writer.WriteStartArray();

            // We sort errors by path to ensure a stable output:
            // - Errors without paths (null) come first
            // - Then errors sorted by path
            foreach (var error in errors.OrderBy(e => e.Path, PathComparer.Instance))
            {
                WriteError(writer, error, options);
            }

            writer.WriteEndArray();
        }
    }

    public static void WriteError(
        JsonWriter writer,
        IError error,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(Message);
        writer.WriteStringValue(error.Message);

        WriteLocations(writer, error.Locations);
        WritePath(writer, error.Path);
        WriteExtensions(writer, error.Extensions, options);

        writer.WriteEndObject();
    }

    public static void WriteExtensions(
        JsonWriter writer,
        IReadOnlyDictionary<string, object?>? dict,
        JsonSerializerOptions options)
    {
        if (dict is { Count: > 0 })
        {
            writer.WritePropertyName(Extensions);
            WriteDictionary(writer, dict, options);
        }
    }

    public static void WriteIncremental(
        JsonWriter writer,
        OperationResult result,
        JsonSerializerOptions options)
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
                WriteIncrementalItem(writer, incremental[i], options);
            }

            writer.WriteEndArray();
        }

        if (result.Completed is { Count: > 0 } completed)
        {
            writer.WritePropertyName(Completed);

            writer.WriteStartArray();

            for (var i = 0; i < completed.Count; i++)
            {
                WriteIncrementalCompletedItem(writer, completed[i], options);
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
        writer.WriteStringValue(item.Id.ToString());

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
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(Id);
        writer.WriteStringValue(item.Id.ToString());

        if (item.Errors is { Count: > 0 })
        {
            WriteErrors(writer, item.Errors, options);
        }

        if (item is IncrementalObjectResult objectResult)
        {
            if (objectResult.SubPath is not null)
            {
                writer.WritePropertyName(SubPath);
                WritePathValue(writer, objectResult.SubPath);
            }

            writer.WritePropertyName(Data);

            if (objectResult.Data.HasValue)
            {
                objectResult.Data.Value.Formatter.WriteDataTo(writer);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
        else if (item is IIncrementalListResult)
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

    private static void WriteIncrementalCompletedItem(
        JsonWriter writer,
        CompletedResult item,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(Id);
        writer.WriteStringValue(item.Id.ToString());

        if (item.Errors is { Count: > 0 })
        {
            WriteErrors(writer, item.Errors, options);
        }

        writer.WriteEndObject();
    }

    private static void WriteLocations(JsonWriter writer, IReadOnlyList<Location>? locations)
    {
        if (locations is { Count: > 0 })
        {
            writer.WritePropertyName(Locations);

            writer.WriteStartArray();

            // We sort locations to ensure a stable output.
            foreach (var location in locations.Order())
            {
                WriteLocation(writer, location);
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

file sealed class PathComparer : IComparer<Path?>
{
    public static readonly PathComparer Instance = new();

    public int Compare(Path? x, Path? y)
    {
        // Null paths should come first
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        return x.CompareTo(y);
    }
}
