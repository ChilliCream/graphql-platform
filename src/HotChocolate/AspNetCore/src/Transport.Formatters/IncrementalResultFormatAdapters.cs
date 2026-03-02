using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Execution.JsonValueFormatter;

namespace HotChocolate.Transport.Formatters;

internal static class IncrementalRfc1ResultFormatAdapter
{
    private static readonly JsonWriterOptions s_jsonWriterOptions = new() { SkipValidation = true };

    public static void WriteIncremental(
        JsonWriter writer,
        OperationResult result,
        JsonSerializerOptions options,
        OperationResultFormatterContext context)
    {
        // We capture the defragmentized document from the first result that carries it.
        // In a stream of incremental results, the document is the same on every result.
        context.InitializeDocument(result.Document);
        var deferLookup = context.DeferSelectionLookup;
        var pending = context.PendingResults;

        if (deferLookup is not null)
        {
            // Next, we cache the initial result's data for later merge with deferred payloads.
            // The v0.1 format of the incremental delivery spec did not deduplicate data,
            // so this adapter needs to reverse the deduplication by capturing individual field values
            // from the initial payload and splicing them back into deferred payloads as needed
            // to reconstruct the full selection for RFC-1 clients.
            CaptureResultData(context, Path.Root, result.Data);
        }

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
                    var pendingPath = pendingResult.Path ?? Path.Root;
                    var path = CombinePath(pendingPath, objectResult.SubPath);

                    if (deferLookup is not null
                        && objectResult.Data.HasValue
                        && !objectResult.Data.Value.IsValueNull)
                    {
                        // We serialize the deferred patch data into the cache buffer so the
                        // backing bytes stay stable for the lifetime of the parsed document.
                        // The same document is then used for both caching field values and
                        // building the merged payload, avoiding a second serialization pass.
                        using var document = SerializeDataToCache(context, objectResult.Data.Value);

                        // Next, we walk every field in the parsed JSON and store it by its path.
                        // Later, when we build the merged payload, we pull these cached values
                        // back in to fill fields that the server did not repeat in the patch.
                        CaptureElement(context, path, document.RootElement);

                        // Try to combine the patch with cached fields into a complete object
                        // for RFC-1 clients; if there is no overlap, we emit the patch as-is.
                        if (TryCreateMergedData(
                                context,
                                pendingPath,
                                path,
                                pendingResult.Label,
                                document,
                                out var mergedData))
                        {
                            (entries ??= []).Add(
                                LegacyIncrementalEntry.ForData(
                                    path,
                                    pendingResult.Label,
                                    data: null,
                                    objectResult.Errors,
                                    mergedData));
                        }
                        else
                        {
                            (entries ??= []).Add(
                                LegacyIncrementalEntry.ForData(
                                    path,
                                    pendingResult.Label,
                                    objectResult.Data,
                                    objectResult.Errors));
                        }
                    }
                    else
                    {
                        (entries ??= []).Add(
                            LegacyIncrementalEntry.ForData(
                                path,
                                pendingResult.Label,
                                objectResult.Data,
                                objectResult.Errors));
                    }
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

    private static bool TryCreateMergedData(
        OperationResultFormatterContext context,
        Path pendingPath,
        Path path,
        string? label,
        JsonDocument patchDocument,
        out ReadOnlyMemorySegment mergedData)
    {
        mergedData = default;

        var deferLookup = context.DeferSelectionLookup;

        if (deferLookup is null
            || !TryResolveSelectionForPath(deferLookup, pendingPath, path, label, out var selection)
            || !selection.HasFields)
        {
            return false;
        }

        if (patchDocument.RootElement.ValueKind is not JsonValueKind.Object)
        {
            return false;
        }

        var cacheBuffer = context.CacheBuffer;
        var start = cacheBuffer.Length;
        var mergedWriter = new JsonWriter(cacheBuffer, s_jsonWriterOptions);
        WriteMergedObject(mergedWriter, context, path, patchDocument.RootElement, selection);
        mergedData = cacheBuffer.GetWrittenMemorySegment(start, cacheBuffer.Length - start);
        return true;
    }

    /// <summary>
    /// Finds the defer selection tree that corresponds to the given path.
    /// First resolves the root selection by label or path, then walks any
    /// remaining path segments to drill into child selections.
    /// </summary>
    private static bool TryResolveSelectionForPath(
        DeferSelectionLookup deferLookup,
        Path pendingPath,
        Path path,
        string? label,
        out DeferSelectionTree selection)
    {
        if (!deferLookup.TryResolveRoot(pendingPath, label, out var selectionPath, out selection)
            && !deferLookup.TryResolveRoot(path, label, out selectionPath, out selection))
        {
            return false;
        }

        if (selectionPath.Equals(path))
        {
            return true;
        }

        var pendingSegments = selectionPath.ToList();
        var pathSegments = path.ToList();

        if (pathSegments.Count < pendingSegments.Count)
        {
            return false;
        }

        for (var i = 0; i < pendingSegments.Count; i++)
        {
            if (!Equals(pendingSegments[i], pathSegments[i]))
            {
                return false;
            }
        }

        for (var i = pendingSegments.Count; i < pathSegments.Count; i++)
        {
            switch (pathSegments[i])
            {
                case string fieldName when selection.TryGetField(fieldName, out var childSelection):
                    selection = childSelection;
                    break;

                case int:
                    continue;

                default:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Writes a JSON object that merges the deferred patch with previously cached fields.
    /// For each field the defer selection expects, we check whether the patch contains it.
    /// If yes, we write it (possibly merging recursively). If not, we try to fill it from the cache.
    /// </summary>
    private static void WriteMergedObject(
        JsonWriter writer,
        OperationResultFormatterContext context,
        Path path,
        JsonElement patchObject,
        DeferSelectionTree selection)
    {
        writer.WriteStartObject();

        for (var i = 0; i < selection.Fields.Count; i++)
        {
            var field = selection.Fields[i];
            var fieldPath = path.Append(field.Name);

            // The patch contains this field, so we write it, merging with cached data if needed.
            if (patchObject.TryGetProperty(field.Name, out var patchValue))
            {
                writer.WritePropertyName(field.Name);
                WriteMergedFieldValue(writer, context, fieldPath, patchValue, field.Selection);
            }
            else
            {
                // The patch does not contain this field, so we try to fill it from the cache.
                TryWriteMissingField(writer, context, field.Name, fieldPath, field.Selection);
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a single field value during the merge.
    /// For leaf fields (no sub-selections), we prefer the cached value from the initial result
    /// because the server may have deduplicated it from the patch.
    /// For composite fields (objects or arrays), we recurse to merge nested data.
    /// </summary>
    private static void WriteMergedFieldValue(
        JsonWriter writer,
        OperationResultFormatterContext context,
        Path path,
        JsonElement patchValue,
        DeferSelectionTree selection)
    {
        // Leaf field: prefer the cached value; fall back to the patch value.
        if (!selection.HasFields)
        {
            if (!TryWriteCachedValue(writer, context, path))
            {
                WriteRawElement(writer, context, patchValue);
            }

            return;
        }

        switch (patchValue.ValueKind)
        {
            case JsonValueKind.Object:
                WriteMergedObject(writer, context, path, patchValue, selection);
                break;

            case JsonValueKind.Array:
                WriteMergedArray(writer, context, path, patchValue, selection);
                break;

            default:
                if (!TryWriteCachedValue(writer, context, path))
                {
                    WriteRawElement(writer, context, patchValue);
                }

                break;
        }
    }

    private static void WriteMergedArray(
        JsonWriter writer,
        OperationResultFormatterContext context,
        Path path,
        JsonElement patchArray,
        DeferSelectionTree selection)
    {
        writer.WriteStartArray();

        var index = 0;
        foreach (var item in patchArray.EnumerateArray())
        {
            var itemPath = path.Append(index++);

            if (item.ValueKind is JsonValueKind.Object)
            {
                WriteMergedObject(writer, context, itemPath, item, selection);
            }
            else if (item.ValueKind is JsonValueKind.Array)
            {
                WriteMergedArray(writer, context, itemPath, item, selection);
            }
            else if (!TryWriteCachedValue(writer, context, itemPath))
            {
                WriteRawElement(writer, context, item);
            }
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Tries to write a field that was not present in the patch by looking it up in the cache.
    /// For leaf fields it writes the cached value directly. For arrays it writes the whole
    /// cached array. For objects it recursively rebuilds from individually cached child fields.
    /// </summary>
    private static bool TryWriteMissingField(
        JsonWriter writer,
        OperationResultFormatterContext context,
        string fieldName,
        Path fieldPath,
        DeferSelectionTree fieldSelection)
    {
        // In the case of a leaf field we write the cached value directly.
        if (!fieldSelection.HasFields)
        {
            if (!context.CachedDataByPath.TryGetValue(fieldPath, out var cachedValue))
            {
                return false;
            }

            writer.WritePropertyName(fieldName);
            writer.WriteRawValue(cachedValue.Segment.Span);
            return true;
        }

        // For Array field we write the whole cached array as raw bytes.
        if (context.CachedDataByPath.TryGetValue(fieldPath, out var cached)
            && cached.ValueKind is JsonValueKind.Array)
        {
            writer.WritePropertyName(fieldName);
            writer.WriteRawValue(cached.Segment.Span);
            return true;
        }

        // With object fields we recursively rebuild from individually cached child fields.
        if (!HasAnyCachedFieldData(context, fieldPath, fieldSelection))
        {
            return false;
        }

        writer.WritePropertyName(fieldName);
        WriteMergedObjectFromCache(writer, context, fieldPath, fieldSelection);
        return true;
    }

    private static void WriteMergedObjectFromCache(
        JsonWriter writer,
        OperationResultFormatterContext context,
        Path path,
        DeferSelectionTree selection)
    {
        writer.WriteStartObject();

        for (var i = 0; i < selection.Fields.Count; i++)
        {
            var field = selection.Fields[i];
            var fieldPath = path.Append(field.Name);
            TryWriteMissingField(writer, context, field.Name, fieldPath, field.Selection);
        }

        writer.WriteEndObject();
    }

    private static bool HasAnyCachedFieldData(
        OperationResultFormatterContext context,
        Path path,
        DeferSelectionTree selection)
    {
        if (context.CachedDataByPath.ContainsKey(path))
        {
            return true;
        }

        for (var i = 0; i < selection.Fields.Count; i++)
        {
            var field = selection.Fields[i];
            var fieldPath = path.Append(field.Name);

            if (context.CachedDataByPath.ContainsKey(fieldPath)
                || (field.Selection.HasFields && HasAnyCachedFieldData(context, fieldPath, field.Selection)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryWriteCachedValue(
        JsonWriter writer,
        OperationResultFormatterContext context,
        Path path)
    {
        if (context.CachedDataByPath.TryGetValue(path, out var cachedValue))
        {
            writer.WriteRawValue(cachedValue.Segment.Span);
            return true;
        }

        return false;
    }

    private static void CaptureResultData(
        OperationResultFormatterContext context,
        Path path,
        OperationResultData? data)
    {
        if (!data.HasValue || data.Value.IsValueNull)
        {
            return;
        }

        using var document = SerializeDataToCache(context, data.Value);
        CaptureElement(context, path, document.RootElement);
    }

    private static void CaptureElement(
        OperationResultFormatterContext context,
        Path path,
        JsonElement element)
    {
        var segment = WriteElementToCache(context.CacheBuffer, element);
        context.CachedDataByPath[path] = new CachedJsonValue(segment, element.ValueKind);

        if (element.ValueKind is JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                CaptureElement(context, path.Append(property.Name), property.Value);
            }
        }
        else if (element.ValueKind is JsonValueKind.Array && !path.IsRoot)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                CaptureElement(context, path.Append(index++), item);
            }
        }
    }

    private static JsonDocument SerializeDataToCache(OperationResultFormatterContext context, OperationResultData data)
    {
        var cacheBuffer = context.CacheBuffer;
        var start = cacheBuffer.Length;
        var writer = new JsonWriter(cacheBuffer, s_jsonWriterOptions);
        data.Formatter.WriteDataTo(writer);
        var segment = cacheBuffer.GetWrittenMemorySegment(start, cacheBuffer.Length - start);
        return JsonDocument.Parse(segment.Memory);
    }

    private static ReadOnlyMemorySegment WriteElementToCache(
        PooledArrayWriter cacheBuffer,
        JsonElement element)
    {
        var start = cacheBuffer.Length;

        using var writer = new Utf8JsonWriter(cacheBuffer);
        element.WriteTo(writer);
        writer.Flush();

        return cacheBuffer.GetWrittenMemorySegment(start, cacheBuffer.Length - start);
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> into raw bytes and writes them to the output.
    /// Uses the scratch buffer as temporary space; it is reset before each use.
    /// </summary>
    private static void WriteRawElement(
        JsonWriter writer,
        OperationResultFormatterContext context,
        JsonElement element)
    {
        var buffer = context.ScratchBuffer;
        buffer.Reset();

        using (var utf8Writer = new Utf8JsonWriter(buffer))
        {
            element.WriteTo(utf8Writer);
        }

        writer.WriteRawValue(buffer.WrittenSpan);
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

                if (entry.RawData.HasValue)
                {
                    writer.WriteRawValue(entry.RawData.Value.Span);
                }
                else if (entry.Data.HasValue)
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
        ReadOnlyMemorySegment? RawData,
        IReadOnlyList<object?>? Items,
        IReadOnlyList<IError>? Errors)
    {
        public static LegacyIncrementalEntry ForData(
            Path path,
            string? label,
            OperationResultData? data,
            IReadOnlyList<IError>? errors,
            ReadOnlyMemorySegment? rawData = null)
            => new(LegacyIncrementalEntryKind.Data, path, label, data, rawData, null, errors);

        public static LegacyIncrementalEntry ForItems(
            Path path,
            string? label,
            IReadOnlyList<object?>? items,
            IReadOnlyList<IError>? errors)
            => new(LegacyIncrementalEntryKind.Items, path, label, null, null, items, errors);
    }
}

internal sealed class DeferSelectionLookup
{
    private readonly Dictionary<string, DeferredSelection> _selectionsByLabel;
    private readonly Dictionary<Path, List<DeferredSelection>> _selectionsByPath;

    private DeferSelectionLookup(
        Dictionary<string, DeferredSelection> selectionsByLabel,
        Dictionary<Path, List<DeferredSelection>> selectionsByPath)
    {
        _selectionsByLabel = selectionsByLabel;
        _selectionsByPath = selectionsByPath;
    }

    /// <summary>
    /// Walks the query document (after fragment inlining) and collects every @defer directive
    /// together with its selected fields. The result is indexed by label and by path
    /// so the format adapter can quickly look up which fields a deferred fragment expects.
    /// </summary>
    public static DeferSelectionLookup Create(DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var selectionsByLabel = new Dictionary<string, DeferredSelection>(StringComparer.Ordinal);
        var selectionsByPath = new Dictionary<Path, List<DeferredSelection>>();

        for (var i = 0; i < document.Definitions.Count; i++)
        {
            if (document.Definitions[i] is not OperationDefinitionNode operation)
            {
                continue;
            }

            CollectDeferredSelections(operation.SelectionSet, Path.Root, selectionsByLabel, selectionsByPath);
        }

        return new DeferSelectionLookup(selectionsByLabel, selectionsByPath);
    }

    public bool TryResolveRoot(
        Path path,
        string? label,
        out Path selectionPath,
        out DeferSelectionTree selection)
    {
        if (!string.IsNullOrEmpty(label)
            && _selectionsByLabel.TryGetValue(label!, out var labeledSelection))
        {
            selectionPath = labeledSelection.Path;
            selection = labeledSelection.Selection;
            return true;
        }

        if (_selectionsByPath.TryGetValue(path, out var selections))
        {
            if (!string.IsNullOrEmpty(label))
            {
                for (var i = 0; i < selections.Count; i++)
                {
                    var candidate = selections[i];

                    if (string.Equals(candidate.Label, label, StringComparison.Ordinal))
                    {
                        selectionPath = candidate.Path;
                        selection = candidate.Selection;
                        return true;
                    }
                }
            }
            else
            {
                for (var i = 0; i < selections.Count; i++)
                {
                    var candidate = selections[i];

                    if (candidate.Label is null)
                    {
                        selectionPath = candidate.Path;
                        selection = candidate.Selection;
                        return true;
                    }
                }

                if (selections.Count == 1)
                {
                    selectionPath = selections[0].Path;
                    selection = selections[0].Selection;
                    return true;
                }
            }
        }

        selectionPath = Path.Root;
        selection = default!;
        return false;
    }

    private static void CollectDeferredSelections(
        SelectionSetNode selectionSet,
        Path currentPath,
        Dictionary<string, DeferredSelection> selectionsByLabel,
        Dictionary<Path, List<DeferredSelection>> selectionsByPath)
    {
        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            switch (selectionSet.Selections[i])
            {
                case FieldNode fieldNode when fieldNode.SelectionSet is not null:
                    CollectDeferredSelections(
                        fieldNode.SelectionSet,
                        currentPath.Append(GetResponseName(fieldNode)),
                        selectionsByLabel,
                        selectionsByPath);
                    break;

                case InlineFragmentNode inlineFragmentNode:
                    if (TryGetDeferLabel(inlineFragmentNode, out var label))
                    {
                        var selection = new DeferSelectionTree();
                        CollectFields(inlineFragmentNode.SelectionSet, selection);

                        if (selection.HasFields)
                        {
                            if (!string.IsNullOrEmpty(label))
                            {
                                selectionsByLabel[label] = new DeferredSelection(currentPath, label, selection);
                            }

                            if (!selectionsByPath.TryGetValue(currentPath, out var selections))
                            {
                                selections = [];
                                selectionsByPath.Add(currentPath, selections);
                            }

                            selections.Add(new DeferredSelection(currentPath, label, selection));
                        }
                    }

                    CollectDeferredSelections(
                        inlineFragmentNode.SelectionSet,
                        currentPath,
                        selectionsByLabel,
                        selectionsByPath);
                    break;
            }
        }
    }

    private static void CollectFields(SelectionSetNode selectionSet, DeferSelectionTree selection)
    {
        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            switch (selectionSet.Selections[i])
            {
                case FieldNode fieldNode:
                    var childSelection = selection.GetOrAddField(GetResponseName(fieldNode));

                    if (fieldNode.SelectionSet is not null)
                    {
                        CollectFields(fieldNode.SelectionSet, childSelection);
                    }

                    break;

                case InlineFragmentNode inlineFragmentNode when !HasDeferDirective(inlineFragmentNode.Directives):
                    CollectFields(inlineFragmentNode.SelectionSet, selection);
                    break;
            }
        }
    }

    private static string GetResponseName(FieldNode field)
        => field.Alias?.Value ?? field.Name.Value;

    private static bool TryGetDeferLabel(InlineFragmentNode node, out string? label)
    {
        label = null;

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];

            if (!directive.Name.Value.Equals("defer", StringComparison.Ordinal))
            {
                continue;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var argument = directive.Arguments[j];

                if (argument.Name.Value.Equals("label", StringComparison.Ordinal)
                    && argument.Value is StringValueNode stringValue)
                {
                    label = stringValue.Value;
                    break;
                }
            }

            return true;
        }

        return false;
    }

    private static bool HasDeferDirective(IReadOnlyList<DirectiveNode> directives)
    {
        for (var i = 0; i < directives.Count; i++)
        {
            if (directives[i].Name.Value.Equals("defer", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct DeferredSelection(Path Path, string? Label, DeferSelectionTree Selection);
}

internal sealed class DeferSelectionTree
{
    private readonly Dictionary<string, DeferSelectionTree> _fieldsByName = new(StringComparer.Ordinal);
    private readonly List<DeferSelectionField> _fields = [];

    public IReadOnlyList<DeferSelectionField> Fields => _fields;

    public bool HasFields => _fields.Count > 0;

    public bool TryGetField(string responseName, out DeferSelectionTree selection)
        => _fieldsByName.TryGetValue(responseName, out selection!);

    public DeferSelectionTree GetOrAddField(string responseName)
    {
        if (!_fieldsByName.TryGetValue(responseName, out var selection))
        {
            selection = new DeferSelectionTree();
            _fieldsByName[responseName] = selection;
            _fields.Add(new DeferSelectionField(responseName, selection));
        }

        return selection;
    }
}

internal readonly record struct DeferSelectionField(string Name, DeferSelectionTree Selection);
