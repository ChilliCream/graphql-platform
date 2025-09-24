using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using static HotChocolate.Fusion.Text.Json.MetaDbMemory;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal struct MetaDb : IDisposable
    {
        private const int SizeOrLengthOffset = 4;
        private const int NumberOfRowsOffset = 8;

        private byte[][] _chunks;
        private int _currentChunk;
        private int _currentPosition;
        private bool _disposed;

        internal int Length { get; private set; }

        internal static MetaDb CreateForEstimatedRows(int estimatedRows)
        {
            var chunksNeeded = Math.Max(4, (estimatedRows / RowsPerChunk) + 1);
            var chunks = new byte[chunksNeeded][];

            chunks[0] = Rent();

            for (var i = 1; i < chunks.Length; i++)
            {
                chunks[i] = [];
            }

            return new MetaDb
            {
                _chunks = chunks,
                _currentChunk = 0,
                _currentPosition = 0,
                Length = 0
            };
        }

        internal void Append(JsonTokenType tokenType, int startLocation, int length)
        {
            Debug.Assert(tokenType is JsonTokenType.StartArray
                or JsonTokenType.StartObject == (length == DbRow.UnknownSize));

            // Check if we need to allocate a new chunk
            if (_currentPosition + DbRow.Size > ChunkSize)
            {
                _currentChunk++;
                _currentPosition = 0;

                // Ensure we have space in the chunks array
                if (_currentChunk >= _chunks.Length)
                {
                    var newChunks = new byte[_chunks.Length * 2][];
                    Array.Copy(_chunks, newChunks, _chunks.Length);

                    for (var i = _chunks.Length; i < newChunks.Length; i++)
                    {
                        newChunks[i] = [];
                    }

                    _chunks = newChunks;
                }

                if (_chunks[_currentChunk].Length == 0)
                {
                    _chunks[_currentChunk] = Rent();
                }
            }

            // Create DbRow and write to chunk
            var row = new DbRow(tokenType, startLocation, length);
            MemoryMarshal.Write(_chunks[_currentChunk].AsSpan(_currentPosition), in row);

            _currentPosition += DbRow.Size;
            Length += DbRow.Size;
        }

        internal void SetLength(int index, int length)
        {
            AssertValidIndex(index);
            Debug.Assert(length >= 0);

            var byteOffset = index;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = (byteOffset % ChunkSize) + SizeOrLengthOffset;

            var destination = _chunks[chunkIndex].AsSpan(localOffset);
            MemoryMarshal.Write(destination, length);
        }

        internal void SetNumberOfRows(int index, int numberOfRows)
        {
            AssertValidIndex(index);
            Debug.Assert(numberOfRows is >= 1 and <= 0x0FFFFFFF);

            var byteOffset = index;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = (byteOffset % ChunkSize) + NumberOfRowsOffset;

            var dataPos = _chunks[chunkIndex].AsSpan(localOffset);
            var current = MemoryMarshal.Read<int>(dataPos);

            // Persist the most significant nybble
            var value = (current & unchecked((int)0xF0000000)) | numberOfRows;
            MemoryMarshal.Write(dataPos, value);
        }

        internal void SetHasComplexChildren(int index)
        {
            AssertValidIndex(index);

            var byteOffset = index;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = (byteOffset % ChunkSize) + SizeOrLengthOffset;

            var dataPos = _chunks[chunkIndex].AsSpan(localOffset);
            var current = MemoryMarshal.Read<int>(dataPos);

            var value = current | unchecked((int)0x80000000);
            MemoryMarshal.Write(dataPos, value);
        }

        internal int FindIndexOfFirstUnsetSizeOrLength(JsonTokenType lookupType)
        {
            Debug.Assert(lookupType == JsonTokenType.StartObject || lookupType == JsonTokenType.StartArray);

            for (var i = Length - DbRow.Size; i >= 0; i -= DbRow.Size)
            {
                var row = Get(i);

                if (row.IsUnknownSize && row.TokenType == lookupType)
                {
                    return i;
                }
            }

            Debug.Fail($"Unable to find expected {lookupType} token");
            return -1;
        }

        internal DbRow Get(int index)
        {
            AssertValidIndex(index);

            var byteOffset = index;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = byteOffset % ChunkSize;

            return MemoryMarshal.Read<DbRow>(_chunks[chunkIndex].AsSpan(localOffset));
        }

        internal JsonTokenType GetJsonTokenType(int index)
        {
            AssertValidIndex(index);

            var byteOffset = index;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = (byteOffset % ChunkSize) + NumberOfRowsOffset;

            var union = MemoryMarshal.Read<uint>(_chunks[chunkIndex].AsSpan(localOffset));
            return (JsonTokenType)(union >> 28);
        }

        [Conditional("DEBUG")]
        private void AssertValidIndex(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index <= Length - DbRow.Size, $"index {index} is out of bounds");
            Debug.Assert(index % DbRow.Size == 0, $"index {index} is not at a record start position");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var chunk in _chunks)
                {
                    if (chunk.Length == 0)
                    {
                        break;
                    }

                    Return(chunk);
                }

                _chunks = [];
                _disposed = true;
            }
        }
    }
}
