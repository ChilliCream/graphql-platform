using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
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
            var formatter = new MinimizedRawJsonFormatter(this, useNameTables: true);
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
    internal ref struct MinimizedRawJsonFormatter
    {
        private const int NoChunk = -1;
        private const int SourceResultFlag = (int)ElementFlags.SourceResult << DbRow.FlagsShift;
        private const int SkippedPropertyFlags =
            (int)(ElementFlags.IsInternal | ElementFlags.IsExcluded) << DbRow.FlagsShift;

        private readonly CompositeResultDocument _document;
        private readonly bool _useNameTables;

        // The chunk of metadata rows that was read last.
        private ref byte _rowChunk;
        private int _rowChunkIndex;
        private int _rowChunkLength;

        // The two chunks of source data that were read last. A key holds the source document
        // id in its low half and the index of the data chunk in its high half.
        private ref byte _sourceChunk0;
        private ref byte _sourceChunk1;
        private long _sourceKey0;
        private long _sourceKey1;
        private int _sourceLength0;
        private int _sourceLength1;

        /// <summary>
        /// Initializes a new formatter for <paramref name="document"/>. When
        /// <paramref name="useNameTables"/> is <c>false</c>, the property names of every object
        /// are resolved from its property rows.
        /// </summary>
        public MinimizedRawJsonFormatter(CompositeResultDocument document, bool useNameTables)
        {
            _document = document;
            _useNameTables = useNameTables;
            _rowChunkIndex = NoChunk;
            _sourceKey0 = NoChunk;
            _sourceKey1 = NoChunk;
        }

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
            var row = _document._metaDb.Get(root);

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
                row = _document._metaDb.Get(cursor);
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
                        _document._sources[row.SourceDocumentId]
                            .WriteRawStringValueTo(ref emitter, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        emitter.WriteStringValue(_document.ReadRawValue(row));
                    }
                    break;

                case ElementTokenType.Number:
                    if (isSourceResult)
                    {
                        _document._sources[row.SourceDocumentId]
                            .WriteRawNumberValueTo(ref emitter, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        emitter.WriteRawValue(_document.ReadRawValue(row));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteSourceValue(ref MinimizedJsonEmitter emitter, DbRow row)
        {
            var sourceDocument = _document._sources[row.SourceDocumentId];
            var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
            sourceDocument.WriteValueTo(ref emitter, sourceCursor);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WriteObject(ref MinimizedJsonEmitter emitter, Cursor start, int numberOfRows)
        {
            Debug.Assert(_document._metaDb.Get(start).TokenType is ElementTokenType.StartObject);

            // The name table of a selection set describes an object whose rows lie in one chunk
            // and whose row count is the one the object template of the selection set writes.
            // Every other object resolves its property names from its property rows.
            if (!_useNameTables)
            {
                WriteObjectSequential(ref emitter, start, numberOfRows);
                return;
            }

            ref var rows = ref GetRow(start, out var availableBytes);

            if ((long)numberOfRows * DbRow.Size > availableBytes)
            {
                WriteObjectSequential(ref emitter, start, numberOfRows);
                return;
            }

            var selectionSetAndFlags = ReadInt32(ref rows, DbRow.SelectionAndFlagsOffset);

            if (DbRow.ReadOperationReferenceType(selectionSetAndFlags) is not OperationReferenceType.SelectionSet)
            {
                WriteObjectSequential(ref emitter, start, numberOfRows);
                return;
            }

            var names = _document._operation
                .GetSelectionSetById(DbRow.ReadOperationReferenceId(selectionSetAndFlags))
                .ObjectTemplate
                .PropertyNames;

            if (names.IsEmpty || PropertyNameTable.GetRowCount(names) != numberOfRows)
            {
                WriteObjectSequential(ref emitter, start, numberOfRows);
                return;
            }

            emitter.WriteStartObject();

            ref var entry = ref Unsafe.Add(ref MemoryMarshal.GetReference(names), PropertyNameTable.HeaderSize);
            ref var propertyRow = ref Unsafe.Add(ref rows, DbRow.Size);
            ref var end = ref Unsafe.Add(ref rows, numberOfRows * DbRow.Size);

            // The rows of the object lie in one chunk, so the packed cursor value advances by
            // plain increments.
            var valueCursor = start.Value + 2;

            for (;
                Unsafe.IsAddressLessThan(ref propertyRow, ref end);
                propertyRow = ref Unsafe.Add(ref propertyRow, 2 * DbRow.Size),
                entry = ref Unsafe.Add(ref entry, PropertyNameTable.EntrySize),
                valueCursor += 2)
            {
                ref var valueRow = ref Unsafe.Add(ref propertyRow, DbRow.Size);
                var selectionAndFlags = ReadInt32(ref propertyRow, DbRow.SelectionAndFlagsOffset);
                var nameLength = ReadInt32(ref entry, PropertyNameTable.LengthOffset);

                // A property row that still holds what the object template wrote names the
                // selection of its table entry, and an entry only has a name token when that
                // selection is neither internal nor excluded.
                if (selectionAndFlags == ReadInt32(ref entry, 0) && nameLength != 0)
                {
                    ref var token = ref Unsafe.Add(ref entry, PropertyNameTable.TokenOffset);
                    ref var value = ref GetValueBlock(ref valueRow, out var valueLength);

                    if (!Unsafe.IsNullRef(ref value))
                    {
                        emitter.WriteSeparatedProperty(ref token, nameLength, ref value, valueLength);
                        continue;
                    }

                    emitter.WriteSeparatedPropertyName(ref token, nameLength);
                }
                else if (!TryWritePropertyName(ref emitter, selectionAndFlags))
                {
                    continue;
                }

                WriteElementValue(ref emitter, Unsafe.ReadUnaligned<DbRow>(ref valueRow), new Cursor(valueCursor));
            }

            emitter.WriteEndObject();
        }

        /// <summary>
        /// Writes the name of the property whose property row holds
        /// <paramref name="selectionAndFlags"/>.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the property is internal or excluded and nothing was written;
        /// otherwise, <c>true</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly bool TryWritePropertyName(ref MinimizedJsonEmitter emitter, int selectionAndFlags)
        {
            if ((selectionAndFlags & SkippedPropertyFlags) != 0)
            {
                return false;
            }

            var selection = _document._operation.GetSelectionById(
                DbRow.ReadOperationReferenceId(selectionAndFlags));
            emitter.WritePropertyName(
                selection.PaddedUtf8JsonPropertyName,
                selection.Utf8JsonPropertyNameLength);
            return true;
        }

        /// <summary>
        /// Gets a reference to the raw JSON of the leaf value in the row at
        /// <paramref name="valueRow"/> when that JSON is at most
        /// <see cref="MinimizedJsonEmitter.BlockSize"/> bytes long, is written as it is and
        /// has that many readable bytes; otherwise, a null reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref byte GetValueBlock(ref byte valueRow, out int length)
        {
            var sourceAndType = ReadInt32(ref valueRow, DbRow.SourceAndTypeOffset);
            var tokenType = DbRow.ReadTokenType(sourceAndType);

            if (tokenType is ElementTokenType.String or ElementTokenType.Number)
            {
                var size = DbRow.ReadSizeOrLength(ReadInt32(ref valueRow, DbRow.SizeOffset));

                if ((ReadInt32(ref valueRow, DbRow.SelectionAndFlagsOffset) & SourceResultFlag) != 0
                    && (uint)size <= MinimizedJsonEmitter.BlockSize)
                {
                    var location = DbRow.ReadLocationOrRows(ReadInt32(ref valueRow, DbRow.LocationOrRowsOffset));
                    ref var chunk = ref GetSourceChunk(
                        DbRow.ReadSourceDocumentId(sourceAndType),
                        location,
                        out var chunkLength);
                    var offset = SourceResultDocument.GetDataChunkOffset(location);

                    if (chunkLength - offset >= MinimizedJsonEmitter.BlockSize)
                    {
                        ref var value = ref Unsafe.Add(ref chunk, offset);

                        // A string that is empty or does not start with a quote is written with
                        // added quotes.
                        if (tokenType is ElementTokenType.Number || (size != 0 && value == JsonConstants.Quote))
                        {
                            length = size;
                            return ref value;
                        }
                    }
                }
            }
            else if (tokenType is ElementTokenType.True)
            {
                length = JsonConstants.TrueValue.Length;
                return ref MinimizedJsonEmitter.TrueBlock;
            }
            else if (tokenType is ElementTokenType.False)
            {
                length = JsonConstants.FalseValue.Length;
                return ref MinimizedJsonEmitter.FalseBlock;
            }
            else if (tokenType is ElementTokenType.Null or ElementTokenType.None)
            {
                length = JsonConstants.NullValue.Length;
                return ref MinimizedJsonEmitter.NullBlock;
            }

            length = 0;
            return ref Unsafe.NullRef<byte>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WriteObjectSequential(ref MinimizedJsonEmitter emitter, Cursor start, int numberOfRows)
        {
            var remainingRows = numberOfRows - 1;

            emitter.WriteStartObject();

            if (remainingRows > 0)
            {
                var operation = _document._operation;
                var reader = _document._metaDb.CreateSequentialReader(start + 1);

                while (true)
                {
                    var property = reader.ReadProperty();

                    if ((property.Flags & (ElementFlags.IsInternal | ElementFlags.IsExcluded)) == ElementFlags.None)
                    {
                        var selection = operation.GetSelectionById(property.SelectionId);
                        emitter.WritePropertyName(
                            selection.PaddedUtf8JsonPropertyName,
                            selection.Utf8JsonPropertyNameLength);

                        WriteElementValue(ref emitter, reader.PeekRow(), reader.Cursor);
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
            Debug.Assert(_document._metaDb.Get(start).TokenType is ElementTokenType.StartArray);

            ref var rows = ref GetRow(start, out var availableBytes);

            if ((long)numberOfRows * DbRow.Size > availableBytes)
            {
                WriteArraySequential(ref emitter, start, numberOfRows);
                return;
            }

            emitter.WriteStartArray();

            for (var i = 1; i < numberOfRows; i++)
            {
                // The rows of the array lie in one chunk, so the packed cursor value advances
                // by plain increments.
                WriteElementValue(
                    ref emitter,
                    Unsafe.ReadUnaligned<DbRow>(ref Unsafe.Add(ref rows, i * DbRow.Size)),
                    new Cursor(start.Value + i));
            }

            emitter.WriteEndArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WriteArraySequential(ref MinimizedJsonEmitter emitter, Cursor start, int numberOfRows)
        {
            var remainingRows = numberOfRows - 1;

            emitter.WriteStartArray();

            if (remainingRows > 0)
            {
                var reader = _document._metaDb.CreateSequentialReader(start + 1);

                while (true)
                {
                    WriteElementValue(ref emitter, reader.PeekRow(), reader.Cursor);

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
        private void WriteElementValue(ref MinimizedJsonEmitter emitter, DbRow row, Cursor cursor)
        {
            switch (row.TokenType)
            {
                case ElementTokenType.Reference:
                    WriteReferencedValue(ref emitter, new Cursor(row.Location));
                    break;

                case ElementTokenType.String
                    when (ElementFlags.SourceResult & row.Flags) == ElementFlags.SourceResult:
                    if (TryGetSourceValue(row, out var stringValue))
                    {
                        emitter.WriteStringValue(stringValue, row.SizeOrLength);
                    }
                    else
                    {
                        _document._sources[row.SourceDocumentId]
                            .WriteRawStringValueTo(ref emitter, row.Location, row.SizeOrLength);
                    }
                    break;

                case ElementTokenType.Number
                    when (ElementFlags.SourceResult & row.Flags) == ElementFlags.SourceResult:
                    if (TryGetSourceValue(row, out var numberValue))
                    {
                        emitter.WriteRawValue(numberValue, row.SizeOrLength);
                    }
                    else
                    {
                        _document._sources[row.SourceDocumentId]
                            .WriteRawNumberValueTo(ref emitter, row.Location, row.SizeOrLength);
                    }
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
                    WriteValue(ref emitter, cursor, row);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteReferencedValue(ref MinimizedJsonEmitter emitter, Cursor cursor)
        {
            ref var row = ref GetRow(cursor, out _);

            if ((ReadInt32(ref row, DbRow.SelectionAndFlagsOffset) & SourceResultFlag) == 0)
            {
                var numberOfRows = DbRow.ReadLocationOrRows(ReadInt32(ref row, DbRow.LocationOrRowsOffset));

                switch (DbRow.ReadTokenType(ReadInt32(ref row, DbRow.SourceAndTypeOffset)))
                {
                    case ElementTokenType.StartObject:
                        WriteObject(ref emitter, cursor, numberOfRows);
                        return;

                    case ElementTokenType.StartArray:
                        WriteArray(ref emitter, cursor, numberOfRows);
                        return;
                }
            }

            WriteValue(ref emitter, cursor, Unsafe.ReadUnaligned<DbRow>(ref row));
        }

        /// <summary>
        /// Gets a reference to the row at <paramref name="cursor"/>.
        /// <paramref name="availableBytes"/> is the number of bytes from that row to the end
        /// of its chunk.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref byte GetRow(Cursor cursor, out int availableBytes)
        {
            var chunkIndex = cursor.Chunk;

            if (chunkIndex != _rowChunkIndex)
            {
                LoadRowChunk(chunkIndex);
            }

            var byteOffset = cursor.ByteOffset;
            availableBytes = _rowChunkLength - byteOffset;
            return ref Unsafe.Add(ref _rowChunk, byteOffset);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LoadRowChunk(int chunkIndex)
        {
            _rowChunk = ref _document._metaDb.GetChunk(chunkIndex, out _rowChunkLength);
            _rowChunkIndex = chunkIndex;
        }

        /// <summary>
        /// Gets the data from the location of the source value in <paramref name="row"/> to
        /// the end of its chunk when the value lies within that chunk.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetSourceValue(DbRow row, out ReadOnlySpan<byte> buffer)
        {
            var location = row.Location;
            ref var chunk = ref GetSourceChunk(row.SourceDocumentId, location, out var chunkLength);
            var offset = SourceResultDocument.GetDataChunkOffset(location);
            var available = chunkLength - offset;

            if (row.SizeOrLength <= available)
            {
                buffer = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref chunk, offset), available);
                return true;
            }

            buffer = default;
            return false;
        }

        /// <summary>
        /// Gets a reference to the first byte of the data chunk of the source document
        /// <paramref name="sourceDocumentId"/> that holds <paramref name="location"/>.
        /// <paramref name="length"/> is the size of the chunk in bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref byte GetSourceChunk(int sourceDocumentId, int location, out int length)
        {
            var key = ((long)SourceResultDocument.GetDataChunkIndex(location) << 32) | (uint)sourceDocumentId;

            if (key == _sourceKey0)
            {
                length = _sourceLength0;
                return ref _sourceChunk0;
            }

            if (key != _sourceKey1)
            {
                LoadSourceChunk(key);
            }

            length = _sourceLength1;
            return ref _sourceChunk1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LoadSourceChunk(long key)
        {
            _sourceChunk0 = ref _sourceChunk1;
            _sourceKey0 = _sourceKey1;
            _sourceLength0 = _sourceLength1;

            _sourceChunk1 = ref _document
                ._sources[(int)key]
                .GetDataChunk((int)(key >>> 32), out _sourceLength1);
            _sourceKey1 = key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadInt32(ref byte row, int offset)
            => Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref row, offset));
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
