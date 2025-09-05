using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace HotChocolate.Text.Json;

public class CompositeJsonDocument
{
    internal struct CompositeMetaDb : IDisposable
    {
        // 6552 rows × 20 bytes
        private const int ChunkSize = 131040;
        private const int RowsPerChunk = 6552;

        internal int Length { get; private set; }
        private byte[][] _chunks;
        private int _currentChunk;
        private int _currentPosition;

        internal static CompositeMetaDb CreateForEstimatedRows(int estimatedRows)
        {
            var chunksNeeded = estimatedRows / RowsPerChunk + 1;
            var chunks = new byte[][chunksNeeded];

            chunks[0] = new byte[ChunkSize];

            return new CompositeMetaDb
            {
                _chunks = chunks,
                _currentChunk = 0,
                _currentPosition = 0,
                Length = 0
            };
        }

        internal void Append(
            JsonTokenType tokenType,
            int location,
            int sizeOrLength,
            int sourceDocumentId,
            int chunkIndex,
            int typeId,
            ElementFlags flags = ElementFlags.None)
        {
            if (_currentPosition + DbRow.Size > ChunkSize)
            {
                _currentChunk++;
                _currentPosition = 0;

                // Allocate new chunk if needed
                if (_currentChunk >= _chunks.Length)
                {
                    Array.Resize(ref _chunks, _chunks.Length * 2);
                }

                _chunks[_currentChunk] ??= new byte[ChunkSize];
            }

            var row = new DbRow(
                tokenType,
                location,
                sizeOrLength,
                sourceDocumentId,
                chunkIndex,
                typeId,
                flags);

            MemoryMarshal.Write(_chunks[_currentChunk].AsSpan(_currentPosition), in row);

            _currentPosition += DbRow.Size;
            Length += DbRow.Size;
        }

        internal DbRow Get(int globalIndex)
        {
            int chunkIndex = (globalIndex / DbRow.Size) / RowsPerChunk;
            int localRowIndex = (globalIndex / DbRow.Size) % RowsPerChunk;
            int byteOffset = localRowIndex * DbRow.Size;

            return MemoryMarshal.Read<DbRow>(_chunks[chunkIndex].AsSpan(byteOffset));
        }

        internal void SetNumberOfRows(int globalIndex, int numberOfRows)
        {
            int chunkIndex = (globalIndex / DbRow.Size) / RowsPerChunk;
            int localRowIndex = (globalIndex / DbRow.Size) % RowsPerChunk;
            int byteOffset = localRowIndex * DbRow.Size + 8; // NumberOfRows offset

            var chunk = _chunks[chunkIndex];
            Span<byte> dataPos = chunk.AsSpan(byteOffset);
            int current = MemoryMarshal.Read<int>(dataPos);

            // Persist the most significant nybble (JsonTokenType)
            int value = (current & unchecked((int)0xF0000000)) | numberOfRows;
            MemoryMarshal.Write(dataPos, ref value);
        }

        public void Dispose()
        {
            // In your case, chunks are just regular arrays, not pooled
            // So disposal is just clearing references
            _chunks = null!;
            Length = 0;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DbRow
    {
        internal const int Size = 20;

        internal const int UnknownSize = -1;

        // Sign bit is currently unassigned
        private readonly int _location;

        // Sign bit is used for "HasComplexChildren" (StartArray)
        private readonly int _sizeOrLengthUnion;

        // Top nybble is JsonTokenType
        // remaining nybbles are the number of rows to skip to get to the next value
        // This isn't limiting on the number of rows, since Span.MaxLength / sizeof(DbRow) can't
        // exceed that range.
        private readonly int _numberOfRowsAndTypeUnion;

        // Pack chunk index + source document ID
        private readonly int _chunkAndSourceInfo;

        // GraphQL type + flags
        private readonly int _typeIdAndFlags;

        internal DbRow(
            JsonTokenType jsonTokenType,
            int location,
            int sizeOrLength,
            int sourceDocumentId,
            int chunkIndex,
            int graphQLTypeId,
            ElementFlags flags = ElementFlags.None)
        {
            Debug.Assert(jsonTokenType is > JsonTokenType.None and <= JsonTokenType.Null);
            Debug.Assert((byte)jsonTokenType < 1 << 4);
            Debug.Assert(location >= 0);
            Debug.Assert(sizeOrLength >= UnknownSize);
            Debug.Assert(sourceDocumentId is >= 0 and <= 0xFFFF);
            Debug.Assert(chunkIndex is >= 0 and <= 0xFFFF);
            Debug.Assert(graphQLTypeId is >= 0 and <= 0xFFFFFF);
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _location = location;
            _sizeOrLengthUnion = sizeOrLength;
            _numberOfRowsAndTypeUnion = (int)jsonTokenType << 28;
            _chunkAndSourceInfo = chunkIndex | (sourceDocumentId << 16);
            _typeIdAndFlags = (int)flags | (graphQLTypeId << 8);
        }

        /// <summary>
        /// The source JSON document that holds the data.
        /// 16 bits = 65K docs
        /// </summary>
        internal int SourceDocumentId => (_chunkAndSourceInfo >> 16) & 0xFFFF;

        /// <summary>
        /// The start chunk into the payload
        /// 16 bits = 65K chunks
        /// </summary>
        internal int ChunkIndex => _chunkAndSourceInfo & 0xFFFF;

        /// <summary>
        /// The start index into the payload
        /// </summary>
        internal int Location => _location;

        /// <summary>
        /// length of text in JSON payload (or number of elements if it's a JSON array)
        /// </summary>
        internal int SizeOrLength => _sizeOrLengthUnion & int.MaxValue;

        internal bool IsUnknownSize => _sizeOrLengthUnion == UnknownSize;

        /// <summary>
        /// String/PropertyName: Unescaping is required.
        /// Array: At least one element is an object/array.
        /// Otherwise; false
        /// </summary>
        internal bool HasComplexChildren => _sizeOrLengthUnion < 0;

        /// <summary>
        /// Number of rows that the current JSON element occupies within the database
        /// </summary>
        internal int NumberOfRows => _numberOfRowsAndTypeUnion & 0x0FFFFFFF;

        /// <summary>
        /// A reference to the GraphQL type associated with the given json element.
        /// 24 bits = 16M+ types
        /// </summary>
        internal int TypeId => (_typeIdAndFlags >> 8) & 0xFFFFFF;

        /// <summary>
        /// Provides additional context.
        /// 8 bits
        /// </summary>
        internal ElementFlags Flags => (ElementFlags)(_typeIdAndFlags & 0xFF);

        internal JsonTokenType TokenType => (JsonTokenType)(unchecked((uint)_numberOfRowsAndTypeUnion) >> 28);

        internal bool IsSimpleValue => TokenType >= JsonTokenType.PropertyName;

        /// <summary>
        /// Sets the number of rows this element spans in the metadb.
        /// Called during metadb construction after calculating tree structure.
        /// </summary>
        internal DbRow WithNumberOfRows(int numberOfRows)
        {
            Debug.Assert(numberOfRows >= 0 && numberOfRows <= 0x0FFFFFFF);

            return new DbRow(
                TokenType,
                _location,
                SizeOrLength,
                SourceDocumentId,
                ChunkIndex,
                TypeId,
                Flags)
            {
                _numberOfRowsAndTypeUnion = (_numberOfRowsAndTypeUnion & unchecked((int)0xF0000000)) | numberOfRows
            };
        }
    }

    [Flags]
    internal enum ElementFlags : byte
    {
        None = 0,
        Invalidated = 1,            // 0x01 - For error propagation
        Local = 2,                  // 0x02 - Distinguish local vs remote fields
        Null = 4,                   // 0x04 - Explicit null marker
        IsNullable = 8,             // 0x08 - Schema information
        Reserved1 = 16,             // 0x10
        Reserved2 = 32,             // 0x20
        Reserved3 = 64,             // 0x40
        Reserved4 = 128             // 0x80
    }
}
