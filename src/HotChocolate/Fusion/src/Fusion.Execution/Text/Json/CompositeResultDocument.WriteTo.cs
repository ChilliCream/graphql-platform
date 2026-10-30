using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteDataTo(JsonWriter jsonWriter)
    {
        if (HasNullMarkers)
        {
            var formatter = new NullMarkerRawJsonFormatter(this, jsonWriter);
            formatter.Write();
        }
        else if (MinimizedRawJsonFormatter.CanWrite(jsonWriter))
        {
            var formatter = new MinimizedRawJsonFormatter(this);
            formatter.Write(jsonWriter);
        }
        else
        {
            var formatter = new RawJsonFormatter(this, jsonWriter);
            formatter.Write();
        }
    }

    internal readonly ref struct RawJsonFormatter(CompositeResultDocument document, JsonWriter writer)
    {
        public void Write()
        {
            var root = Cursor.CreateZero();
            var row = document._metaDb.Get(root);

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.Invalidated & row.Flags) == ElementFlags.Invalidated)
            {
                writer.WriteNullValue();
            }
            else
            {
                WriteObject(root, row);
            }
        }

        public void WriteValue(Cursor cursor, DbRow row)
        {
            var tokenType = row.TokenType;

            // Inline reference resolution
            if (tokenType is ElementTokenType.Reference)
            {
                cursor = new Cursor(row.Location);
                row = document._metaDb.Get(cursor);
                tokenType = row.TokenType;
            }

            Debug.Assert(tokenType is not ElementTokenType.Reference);
            Debug.Assert(tokenType is not ElementTokenType.EndObject);
            Debug.Assert(tokenType is not ElementTokenType.EndArray);
            var isSourceResult = (ElementFlags.SourceResult & row.Flags) == ElementFlags.SourceResult;

            switch (tokenType)
            {
                case ElementTokenType.StartObject:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteObject(cursor, row);
                    }
                    break;

                case ElementTokenType.StartArray:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteArray(cursor, row);
                    }
                    break;

                case ElementTokenType.None:
                case ElementTokenType.Null:
                    writer.WriteNullValue();
                    break;

                case ElementTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;

                case ElementTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;

                case ElementTokenType.String:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawStringValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteStringValue(document.ReadRawValue(row), skipEscaping: true);
                    }
                    break;

                case ElementTokenType.Number:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawNumberValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteNumberValue(document.ReadRawValue(row));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteSourceValue(DbRow row)
        {
            var sourceDocument = document._sources[row.SourceDocumentId];
            // Reconstruct the source cursor from stored Location (Chunk) and SizeOrLength (Row)
            var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
            var formatter = new SourceResultDocument.RawJsonFormatter(sourceDocument, writer);
            formatter.WriteValue(sourceCursor);
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartObject);

            var remainingRows = startRow.NumberOfRows - 1;
            var operation = document._operation;

            writer.WriteStartObject();

            if (remainingRows == 0)
            {
                writer.WriteEndObject();
                return;
            }

            var reader = document._metaDb.CreateSequentialReader(start + 1);

            while (remainingRows > 0)
            {
                var property = reader.ReadProperty();

                if ((ElementFlags.IsInternal & property.Flags) == ElementFlags.IsInternal
                    || (ElementFlags.IsExcluded & property.Flags) == ElementFlags.IsExcluded)
                {
                    remainingRows -= 2;

                    if (remainingRows > 0)
                    {
                        reader.Advance(1);
                    }

                    continue;
                }

                writer.WritePropertyName(
                    operation
                        .GetSelectionById(property.SelectionId)
                        .Utf8ResponseName);

                WriteValue(reader.Cursor, reader.PeekRow());
                remainingRows -= 2;

                if (remainingRows > 0)
                {
                    reader.Advance(1);
                }
            }

            writer.WriteEndObject();
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartArray);

            var remainingRows = startRow.NumberOfRows - 1;

            writer.WriteStartArray();

            if (remainingRows == 0)
            {
                writer.WriteEndArray();
                return;
            }

            var reader = document._metaDb.CreateSequentialReader(start + 1);

            while (remainingRows > 0)
            {
                WriteValue(reader.Cursor, reader.PeekRow());
                remainingRows--;

                if (remainingRows > 0)
                {
                    reader.Advance(1);
                }
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Writes the document as minimized JSON directly into the buffer of a
    /// <see cref="JsonWriter"/> that neither indents nor omits nulls.
    /// </summary>
    internal readonly ref struct MinimizedRawJsonFormatter(CompositeResultDocument document)
    {
        public static bool CanWrite(JsonWriter writer)
        {
            var options = writer.Options;

            // Response names are GraphQL names, which these encoders never escape.
            return !options.Indented
                && writer.NullIgnoreCondition is JsonNullIgnoreCondition.None
                && (options.Encoder is null
                    || ReferenceEquals(options.Encoder, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
                    || ReferenceEquals(options.Encoder, JavaScriptEncoder.Default));
        }

        public void Write(JsonWriter writer)
        {
            Debug.Assert(CanWrite(writer));

            var root = Cursor.CreateZero();
            var row = document._metaDb.Get(root);

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.Invalidated & row.Flags) == ElementFlags.Invalidated)
            {
                writer.WriteNullValue();
                return;
            }

            var maxDepth = writer.Options.MaxDepth == 0 ? 64 : writer.Options.MaxDepth;

            writer.WriteRawValueStart([]);
            var emitter = new MinimizedJsonEmitter(writer.InnerWriter, writer.CurrentDepth, maxDepth);
            WriteObject(ref emitter, root, row.NumberOfRows);
            emitter.Flush();
            writer.WriteRawValueEnd(JsonTokenType.EndObject);
        }

        private void WriteValue(ref MinimizedJsonEmitter emitter, Cursor cursor, DbRow row)
        {
            var tokenType = row.TokenType;

            if (tokenType is ElementTokenType.Reference)
            {
                cursor = new Cursor(row.Location);
                row = document._metaDb.Get(cursor);
                tokenType = row.TokenType;
            }

            Debug.Assert(tokenType is not ElementTokenType.Reference);
            Debug.Assert(tokenType is not ElementTokenType.EndObject);
            Debug.Assert(tokenType is not ElementTokenType.EndArray);
            var isSourceResult = (ElementFlags.SourceResult & row.Flags) == ElementFlags.SourceResult;

            switch (tokenType)
            {
                case ElementTokenType.StartObject:
                    if (isSourceResult)
                    {
                        WriteSourceValue(ref emitter, row);
                    }
                    else
                    {
                        WriteObject(ref emitter, cursor, row.NumberOfRows);
                    }
                    break;

                case ElementTokenType.StartArray:
                    if (isSourceResult)
                    {
                        WriteSourceValue(ref emitter, row);
                    }
                    else
                    {
                        WriteArray(ref emitter, cursor, row.NumberOfRows);
                    }
                    break;

                case ElementTokenType.None:
                case ElementTokenType.Null:
                    emitter.WriteRawValue(JsonConstants.NullValue);
                    break;

                case ElementTokenType.True:
                    emitter.WriteRawValue(JsonConstants.TrueValue);
                    break;

                case ElementTokenType.False:
                    emitter.WriteRawValue(JsonConstants.FalseValue);
                    break;

                case ElementTokenType.String:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawStringValueTo(ref emitter, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        emitter.WriteStringValue(document.ReadRawValue(row));
                    }
                    break;

                case ElementTokenType.Number:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawNumberValueTo(ref emitter, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        emitter.WriteRawValue(document.ReadRawValue(row));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteSourceValue(ref MinimizedJsonEmitter emitter, DbRow row)
        {
            var sourceDocument = document._sources[row.SourceDocumentId];
            var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
            sourceDocument.WriteValueTo(ref emitter, sourceCursor);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WriteObject(ref MinimizedJsonEmitter emitter, Cursor start, int numberOfRows)
        {
            Debug.Assert(document._metaDb.Get(start).TokenType is ElementTokenType.StartObject);

            var remainingRows = numberOfRows - 1;

            emitter.WriteStartObject();

            if (remainingRows > 0)
            {
                var operation = document._operation;
                var reader = document._metaDb.CreateSequentialReader(start + 1);

                while (true)
                {
                    var property = reader.ReadProperty();

                    if ((property.Flags & (ElementFlags.IsInternal | ElementFlags.IsExcluded)) == ElementFlags.None)
                    {
                        var selection = operation.GetSelectionById(property.SelectionId);
                        emitter.WritePropertyName(
                            selection.PaddedUtf8JsonPropertyName,
                            selection.Utf8JsonPropertyNameLength);

                        WriteElementValue(ref emitter, ref reader);
                    }

                    remainingRows -= 2;

                    if (remainingRows <= 0)
                    {
                        break;
                    }

                    reader.Advance(1);
                }
            }

            emitter.WriteEndObject();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WriteArray(ref MinimizedJsonEmitter emitter, Cursor start, int numberOfRows)
        {
            Debug.Assert(document._metaDb.Get(start).TokenType is ElementTokenType.StartArray);

            var remainingRows = numberOfRows - 1;

            emitter.WriteStartArray();

            if (remainingRows > 0)
            {
                var reader = document._metaDb.CreateSequentialReader(start + 1);

                while (true)
                {
                    WriteElementValue(ref emitter, ref reader);

                    if (--remainingRows == 0)
                    {
                        break;
                    }

                    reader.Advance(1);
                }
            }

            emitter.WriteEndArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteElementValue(ref MinimizedJsonEmitter emitter, ref MetaDb.SequentialReader reader)
        {
            var row = reader.PeekRow();

            switch (row.TokenType)
            {
                case ElementTokenType.Reference:
                    WriteReferencedValue(ref emitter, new Cursor(row.Location));
                    break;

                case ElementTokenType.String
                    when (ElementFlags.SourceResult & row.Flags) == ElementFlags.SourceResult:
                    document._sources[row.SourceDocumentId]
                        .WriteRawStringValueTo(ref emitter, row.Location, row.SizeOrLength);
                    break;

                case ElementTokenType.Number
                    when (ElementFlags.SourceResult & row.Flags) == ElementFlags.SourceResult:
                    document._sources[row.SourceDocumentId]
                        .WriteRawNumberValueTo(ref emitter, row.Location, row.SizeOrLength);
                    break;

                case ElementTokenType.True:
                    emitter.WriteRawValue(JsonConstants.TrueValue);
                    break;

                case ElementTokenType.False:
                    emitter.WriteRawValue(JsonConstants.FalseValue);
                    break;

                case ElementTokenType.None:
                case ElementTokenType.Null:
                    emitter.WriteRawValue(JsonConstants.NullValue);
                    break;

                default:
                    WriteValue(ref emitter, reader.Cursor, row);
                    break;
            }
        }

        private void WriteReferencedValue(ref MinimizedJsonEmitter emitter, Cursor cursor)
        {
            var row = document._metaDb.Get(cursor);

            if ((ElementFlags.SourceResult & row.Flags) == ElementFlags.None)
            {
                switch (row.TokenType)
                {
                    case ElementTokenType.StartObject:
                        WriteObject(ref emitter, cursor, row.NumberOfRows);
                        return;

                    case ElementTokenType.StartArray:
                        WriteArray(ref emitter, cursor, row.NumberOfRows);
                        return;
                }
            }

            WriteValue(ref emitter, cursor, row);
        }
    }

    internal readonly ref struct NullMarkerRawJsonFormatter(
        CompositeResultDocument document,
        JsonWriter writer)
    {
        public void Write()
        {
            var root = Cursor.CreateZero();
            var row = document._metaDb.Get(root);

            if (row.IsNullMarker
                || row.TokenType is ElementTokenType.Null
                || (ElementFlags.Invalidated & row.Flags) is ElementFlags.Invalidated)
            {
                writer.WriteNullValue();
            }
            else
            {
                WriteObject(root, row);
            }
        }

        public void WriteValue(Cursor cursor, DbRow row)
        {
            if (row.IsNullMarker)
            {
                writer.WriteNullValue();
                return;
            }

            var tokenType = row.TokenType;

            // The null marker belongs to this logical value/reference. Resolve only after
            // checking it so another path that references the same object remains unaffected.
            if (tokenType is ElementTokenType.Reference)
            {
                cursor = new Cursor(row.Location);
                row = document._metaDb.Get(cursor);
                tokenType = row.TokenType;
            }

            Debug.Assert(tokenType is not ElementTokenType.Reference);
            Debug.Assert(tokenType is not ElementTokenType.EndObject);
            Debug.Assert(tokenType is not ElementTokenType.EndArray);
            var isSourceResult = (ElementFlags.SourceResult & row.Flags) is ElementFlags.SourceResult;

            switch (tokenType)
            {
                case ElementTokenType.StartObject:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteObject(cursor, row);
                    }
                    break;

                case ElementTokenType.StartArray:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteArray(cursor, row);
                    }
                    break;

                case ElementTokenType.None:
                case ElementTokenType.Null:
                    writer.WriteNullValue();
                    break;

                case ElementTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;

                case ElementTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;

                case ElementTokenType.String:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawStringValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteStringValue(document.ReadRawValue(row), skipEscaping: true);
                    }
                    break;

                case ElementTokenType.Number:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawNumberValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteNumberValue(document.ReadRawValue(row));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteSourceValue(DbRow row)
        {
            var sourceDocument = document._sources[row.SourceDocumentId];
            var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
            var formatter = new SourceResultDocument.RawJsonFormatter(sourceDocument, writer);
            formatter.WriteValue(sourceCursor);
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartObject);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            writer.WriteStartObject();

            while (current < end)
            {
                var row = document._metaDb.Get(current);
                Debug.Assert(row.TokenType is ElementTokenType.PropertyName);

                if ((ElementFlags.IsInternal & row.Flags) is ElementFlags.IsInternal
                    || (ElementFlags.IsExcluded & row.Flags) is ElementFlags.IsExcluded)
                {
                    current += 2;
                    continue;
                }

                writer.WritePropertyName(document.ReadRawValue(row));

                current++;
                row = document._metaDb.Get(current);
                WriteValue(current, row);

                current++;
            }

            writer.WriteEndObject();
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartArray);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            writer.WriteStartArray();

            while (current < end)
            {
                var row = document._metaDb.Get(current);
                WriteValue(current, row);
                current++;
            }

            writer.WriteEndArray();
        }
    }
}
