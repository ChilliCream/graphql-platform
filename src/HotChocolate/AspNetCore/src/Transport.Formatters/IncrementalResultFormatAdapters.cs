using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Text.Json;
using static HotChocolate.Execution.JsonValueFormatter;

namespace HotChocolate.Transport.Formatters;

internal static class IncrementalRfc1ResultFormatAdapter
{
    public static void WriteIncremental(
        JsonWriter writer,
        OperationResult result,
        JsonSerializerOptions options,
        OperationResultFormatterContext context)
    {
        var pending = context.PendingResults;

        if (result.Pending is { Count: > 0 } pendingResults)
        {
            for (var i = 0; i < pendingResults.Count; i++)
            {
                var item = pendingResults[i];
                pending[item.Id] = new PendingResultState(item.Path, item.Label);
            }
        }

        List<LegacyIncrementalEntry>? entries = null;

        if (result.Incremental is { Count: > 0 } incrementalResults)
        {
            for (var i = 0; i < incrementalResults.Count; i++)
            {
                var item = incrementalResults[i];

                if (!pending.TryGetValue(item.Id, out var pendingResult))
                {
                    throw new InvalidOperationException(
                        $"Received incremental result for unannounced id '{item.Id}'.");
                }

                if (item is IncrementalObjectResult objectResult)
                {
                    var path = CombinePath(pendingResult.Path ?? Path.Root, objectResult.SubPath);
                    (entries ??= []).Add(
                        LegacyIncrementalEntry.ForData(
                            path,
                            pendingResult.Label,
                            objectResult.Data,
                            objectResult.Errors));
                }
                else if (item is IIncrementalListResult listResult)
                {
                    (entries ??= []).Add(
                        LegacyIncrementalEntry.ForItems(
                            pendingResult.Path ?? Path.Root,
                            pendingResult.Label,
                            listResult.Items,
                            listResult.Errors));
                }
            }
        }

        if (result.Completed is { Count: > 0 } completedResults)
        {
            for (var i = 0; i < completedResults.Count; i++)
            {
                var completed = completedResults[i];

                if (completed.Errors is { Count: > 0 })
                {
                    if (!pending.TryGetValue(completed.Id, out var pendingResult))
                    {
                        throw new InvalidOperationException(
                            $"Received completed result for unannounced id '{completed.Id}'.");
                    }

                    (entries ??= []).Add(
                        LegacyIncrementalEntry.ForData(
                            pendingResult.Path ?? Path.Root,
                            pendingResult.Label,
                            data: null,
                            completed.Errors));
                }

                pending.Remove(completed.Id);
            }
        }

        if (entries is { Count: > 0 })
        {
            writer.WritePropertyName(ResultFieldNames.Incremental);
            writer.WriteStartArray();

            for (var i = 0; i < entries.Count; i++)
            {
                WriteLegacyIncrementalEntry(writer, entries[i], options);
            }

            writer.WriteEndArray();
        }

        if (result.HasNext.HasValue)
        {
            writer.WritePropertyName(ResultFieldNames.HasNext);
            writer.WriteBooleanValue(result.HasNext.Value);
        }
    }

    private static void WriteLegacyIncrementalEntry(
        JsonWriter writer,
        LegacyIncrementalEntry entry,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (entry.Errors is { Count: > 0 } errors)
        {
            WriteErrors(writer, errors, options);
        }

        switch (entry.Kind)
        {
            case LegacyIncrementalEntryKind.Data:
                writer.WritePropertyName(ResultFieldNames.Data);

                if (entry.Data.HasValue)
                {
                    entry.Data.Value.Formatter.WriteDataTo(writer);
                }
                else
                {
                    writer.WriteNullValue();
                }

                break;

            case LegacyIncrementalEntryKind.Items:
                writer.WritePropertyName(ResultFieldNames.Items);
                WriteItems(writer, entry.Items, options);
                break;
        }

        writer.WritePropertyName(ResultFieldNames.Path);
        WriteValue(writer, entry.Path, options);

        if (!string.IsNullOrEmpty(entry.Label))
        {
            writer.WritePropertyName(ResultFieldNames.Label);
            writer.WriteStringValue(entry.Label);
        }

        writer.WriteEndObject();
    }

    private static void WriteItems(
        JsonWriter writer,
        IReadOnlyList<object?>? items,
        JsonSerializerOptions options)
    {
        if (items is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();

        for (var i = 0; i < items.Count; i++)
        {
            WriteValue(writer, items[i], options);
        }

        writer.WriteEndArray();
    }

    private static Path CombinePath(Path path, Path? subPath)
    {
        if (subPath is null || subPath.IsRoot)
        {
            return path;
        }

        return path.Append(subPath);
    }

    private enum LegacyIncrementalEntryKind
    {
        Data,
        Items
    }

    private readonly record struct LegacyIncrementalEntry(
        LegacyIncrementalEntryKind Kind,
        Path Path,
        string? Label,
        OperationResultData? Data,
        IReadOnlyList<object?>? Items,
        IReadOnlyList<IError>? Errors)
    {
        public static LegacyIncrementalEntry ForData(
            Path path,
            string? label,
            OperationResultData? data,
            IReadOnlyList<IError>? errors)
            => new(LegacyIncrementalEntryKind.Data, path, label, data, null, errors);

        public static LegacyIncrementalEntry ForItems(
            Path path,
            string? label,
            IReadOnlyList<object?>? items,
            IReadOnlyList<IError>? errors)
            => new(LegacyIncrementalEntryKind.Items, path, label, null, items, errors);
    }
}
