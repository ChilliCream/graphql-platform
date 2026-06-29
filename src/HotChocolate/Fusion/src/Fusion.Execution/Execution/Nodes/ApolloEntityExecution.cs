using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using JsonWriter = HotChocolate.Text.Json.JsonWriter;

namespace HotChocolate.Fusion.Execution.Nodes;

internal static class ApolloEntityExecution
{
    private static readonly JsonWriterOptions s_jsonWriterOptions = new() { Indented = false };
    private static ReadOnlySpan<byte> DataProperty => "data"u8;
    private static ReadOnlySpan<byte> EntitiesProperty => "_entities"u8;
    private static ReadOnlySpan<byte> TypenameProperty => "__typename"u8;
    private static ReadOnlySpan<byte> RepresentationsProperty => "representations"u8;

    public static VariableValues CreateVariables(
        ImmutableArray<VariableValues> variableSets,
        ApolloEntityOperation operation,
        out ChunkedArrayWriter buffer)
    {
        buffer = new ChunkedArrayWriter();
        var writer = new JsonWriter(buffer, s_jsonWriterOptions);
        var start = buffer.Position;

        writer.WriteStartObject();
        writer.WritePropertyName(RepresentationsProperty);
        WriteRepresentationsArray(writer, variableSets, operation, emptyWhenNoVariables: false);
        writer.WriteEndObject();

        var length = buffer.Position - start;
        return new VariableValues(CompactPath.Root, JsonSegment.Create(buffer, start, length));
    }

    public static VariableValues CreateBatchVariables(
        ImmutableArray<VariableValues>[] variableSetsByOperation,
        bool[] activeOperations,
        ApolloEntityOperation[] operations,
        out ChunkedArrayWriter buffer)
    {
        buffer = new ChunkedArrayWriter();
        var writer = new JsonWriter(buffer, s_jsonWriterOptions);
        var start = buffer.Position;

        writer.WriteStartObject();

        for (var i = 0; i < operations.Length; i++)
        {
            WriteRawAsciiPropertyName(writer, "r" + i);
            WriteRepresentationsArray(
                writer,
                variableSetsByOperation[i],
                operations[i],
                emptyWhenNoVariables: !activeOperations[i]);
        }

        writer.WriteEndObject();

        var length = buffer.Position - start;
        return new VariableValues(CompactPath.Root, JsonSegment.Create(buffer, start, length));
    }

    public static void SplitEntities(
        SourceSchemaResult rawResult,
        ImmutableArray<VariableValues> variables,
        List<SourceSchemaResult> results)
    {
        var dataElement = rawResult.Data;

        if (dataElement.ValueKind != JsonValueKind.Object
            || !dataElement.TryGetProperty(EntitiesProperty, out var entitiesElement)
            || entitiesElement.ValueKind != JsonValueKind.Array)
        {
            AddRawResult(rawResult, variables, results);
            return;
        }

        var entityCount = entitiesElement.GetArrayLength();
        const bool ownsSourceDocument = true;
        var ownerAssigned = false;

        for (var i = 0; i < entityCount; i++)
        {
            var entity = entitiesElement[i];
            GetResultPath(variables, i, out var path, out var additionalPaths);
            var ownsDocument = ownsSourceDocument && !ownerAssigned;

            results.Add(CreateEntityResult(
                rawResult,
                path,
                additionalPaths,
                entity,
                ownsDocument));

            ownerAssigned |= ownsDocument;
        }

        if (!ownerAssigned)
        {
            rawResult.Dispose();
        }
    }

    public static void SplitAliasedEntities(
        IMemoryArena arena,
        SourceSchemaResult rawResult,
        SourceResultElement dataElement,
        ImmutableArray<VariableValues> variables,
        string aliasName,
        List<SourceSchemaResult> results,
        ref bool sourceDocumentOwnerAssigned)
    {
        Span<byte> aliasNameUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(aliasName)];
        Encoding.UTF8.GetBytes(aliasName.AsSpan(), aliasNameUtf8);

        if (dataElement.ValueKind != JsonValueKind.Object
            || !dataElement.TryGetProperty(aliasNameUtf8, out var entitiesElement)
            || entitiesElement.ValueKind != JsonValueKind.Array)
        {
            GetResultPath(variables, 0, out var path, out var additionalPaths);
            results.Add(CreateNullResult(arena, path, additionalPaths));
            return;
        }

        var entityCount = entitiesElement.GetArrayLength();
        const bool ownsSourceDocument = true;

        for (var i = 0; i < entityCount; i++)
        {
            var entity = entitiesElement[i];
            GetResultPath(variables, i, out var path, out var additionalPaths);
            var ownsDocument = ownsSourceDocument && !sourceDocumentOwnerAssigned;

            results.Add(CreateEntityResult(
                rawResult,
                path,
                additionalPaths,
                entity,
                ownsDocument));

            sourceDocumentOwnerAssigned |= ownsDocument;
        }
    }

    private static void WriteRepresentationsArray(
        JsonWriter writer,
        ImmutableArray<VariableValues> variableSets,
        ApolloEntityOperation operation,
        bool emptyWhenNoVariables)
    {
        writer.WriteStartArray();

        if (variableSets.IsDefaultOrEmpty)
        {
            if (!emptyWhenNoVariables)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(TypenameProperty);
                writer.WriteStringValue(operation.EntityTypeName);
                writer.WriteEndObject();
            }
        }
        else
        {
            for (var i = 0; i < variableSets.Length; i++)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(TypenameProperty);
                writer.WriteStringValue(operation.EntityTypeName);
                WriteRepresentationFields(writer, variableSets[i], operation.RepresentationFields);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteRepresentationFields(
        JsonWriter writer,
        VariableValues variableValues,
        ApolloRepresentationField[] fields)
    {
        if (variableValues.Values.IsEmpty)
        {
            return;
        }

        var values = variableValues.Values.AsSequence();
        var reader = new Utf8JsonReader(values);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return;
        }

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var field = FindField(ref reader, fields);

            if (!reader.Read())
            {
                return;
            }

            if (field is null)
            {
                reader.Skip();
                continue;
            }

            if (field.FieldName.Length == 0)
            {
                SpreadObjectValue(writer, values, ref reader);
            }
            else
            {
                writer.WritePropertyName(field.FieldNameUtf8);
                WriteCurrentRawValue(writer, values, ref reader);
            }
        }
    }

    private static ApolloRepresentationField? FindField(
        ref Utf8JsonReader reader,
        ApolloRepresentationField[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (reader.ValueTextEquals(fields[i].VariableNameUtf8))
            {
                return fields[i];
            }
        }

        return null;
    }

    private static void SpreadObjectValue(
        JsonWriter writer,
        ReadOnlySequence<byte> values,
        ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString()!;

            if (!reader.Read())
            {
                return;
            }

            writer.WritePropertyName(propertyName);
            WriteCurrentRawValue(writer, values, ref reader);
        }
    }

    private static void WriteCurrentRawValue(
        JsonWriter writer,
        ReadOnlySequence<byte> values,
        ref Utf8JsonReader reader)
    {
        var start = reader.TokenStartIndex;
        reader.Skip();
        var length = reader.BytesConsumed - start;
        WriteRawJsonValue(writer, values.Slice(start, length));
    }

    private static void WriteRawJsonValue(JsonWriter writer, ReadOnlySequence<byte> value)
    {
        if (value.IsSingleSegment)
        {
            writer.WriteRawValue(value.FirstSpan);
            return;
        }

        writer.WriteRawValue(value.ToArray());
    }

    private static SourceSchemaResult CreateEntityResult(
        SourceSchemaResult rawResult,
        CompactPath path,
        CompactPathSegment additionalPaths,
        SourceResultElement entity,
        bool ownsDocument)
    {
        return rawResult.WithData(path, entity, ownsDocument, additionalPaths);
    }

    private static SourceSchemaResult CreateNullResult(
        IMemoryArena arena,
        CompactPath path,
        CompactPathSegment additionalPaths)
    {
        using var buffer = new ArenaBufferWriter(arena);
        var writer = new JsonWriter(buffer, s_jsonWriterOptions);

        writer.WriteStartObject();
        writer.WritePropertyName(DataProperty);
        writer.WriteNullValue();
        writer.WriteEndObject();

        var document = SourceResultDocument.ParseFilled(
            arena,
            buffer.Segments,
            buffer.UsedChunks,
            buffer.LastLength);

        return additionalPaths.IsDefaultOrEmpty
            ? new SourceSchemaResult(path, document)
            : new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
    }

    private static void AddRawResult(
        SourceSchemaResult rawResult,
        ImmutableArray<VariableValues> variables,
        List<SourceSchemaResult> results)
    {
        GetResultPath(variables, 0, out var path, out var additionalPaths);
        results.Add(rawResult.WithOwnedPath(path, additionalPaths));
    }

    private static void GetResultPath(
        ImmutableArray<VariableValues> variables,
        int index,
        out CompactPath path,
        out CompactPathSegment additionalPaths)
    {
        if (variables.IsDefaultOrEmpty || index >= variables.Length)
        {
            path = CompactPath.Root;
            additionalPaths = default;
            return;
        }

        path = variables[index].Path;
        additionalPaths = variables[index].AdditionalPaths;
    }

    private static void WriteRawAsciiPropertyName(JsonWriter writer, string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        Encoding.UTF8.GetBytes(value.AsSpan(), buffer);
        writer.WritePropertyName(buffer);
    }
}
