using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private MetaDb _parsedData;
    private readonly byte[][] _dataChunks;
    private readonly int _usedChunks;
    private readonly bool _pooledMemory;
    private bool _disposed;

    private SourceResultDocument(MetaDb parsedData, byte[][] dataChunks, int usedChunks, bool pooledMemory)
    {
        _parsedData = parsedData;
        _dataChunks = dataChunks;
        _usedChunks = usedChunks;
        _pooledMemory = pooledMemory;
        Root = new SourceResultElement(this, Cursor.Zero);
    }

    internal int Id { get; set; } = -1;

    public SourceResultElement Root { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal JsonTokenType GetElementTokenType(Cursor cursor)
        => _parsedData.GetJsonTokenType(cursor);

    internal int GetArrayLength(Cursor cursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(cursor);

        CheckExpectedType(JsonTokenType.StartArray, row.TokenType);

        return row.SizeOrLength;
    }

    internal int GetPropertyCount(Cursor cursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(cursor);

        CheckExpectedType(JsonTokenType.StartObject, row.TokenType);

        return row.SizeOrLength;
    }

    internal SourceResultElement GetArrayIndexElement(Cursor startCursor, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(startCursor);

        CheckExpectedType(JsonTokenType.StartArray, row.TokenType);

        var arrayLength = row.SizeOrLength;

        if ((uint)arrayIndex >= (uint)arrayLength)
        {
            throw new IndexOutOfRangeException();
        }

        if (!row.HasComplexChildren)
        {
            // Since we wouldn't be here without having completed the document parse, and we
            // already vetted the index against the length, this new index will always be
            // within the table.
            var target = startCursor + (arrayIndex + 1);
            return new SourceResultElement(this, target);
        }

        var elementCount = 0;
        var cursor = startCursor + 1;

        while (true)
        {
            if (elementCount == arrayIndex)
            {
                return new SourceResultElement(this, cursor);
            }

            var child = _parsedData.Get(cursor);

            if (child.IsSimpleValue)
            {
                cursor++;
            }
            else
            {
                cursor += child.NumberOfRows;
            }

            elementCount++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteRawValueTo(Utf8JsonWriter writer, DbRow row)
    {
        if (row.TokenType is JsonTokenType.String)
        {
            writer.WriteRawValue(ReadRawValue(row.Location - 1, row.SizeOrLength + 2), skipInputValidation: true);
            return;
        }

        writer.WriteRawValue(ReadRawValue(row.Location, row.SizeOrLength), skipInputValidation: true);
    }

    internal void WriteRawValueTo(IBufferWriter<byte> writer, int location, int size)
    {
        var startChunkIndex = location / JsonMemory.BufferSize;
        var offsetInStartChunk = location % JsonMemory.BufferSize;

        if (offsetInStartChunk + size <= JsonMemory.BufferSize)
        {
            var span = writer.GetSpan(size);
            _dataChunks[startChunkIndex].AsSpan(offsetInStartChunk, size).CopyTo(span);
            writer.Advance(size);
            return;
        }

        var bytesRead = 0;
        var currentLocation = location;

        while (bytesRead < size)
        {
            var chunkIndex = currentLocation / JsonMemory.BufferSize;
            var offsetInChunk = currentLocation % JsonMemory.BufferSize;
            var chunk = _dataChunks[chunkIndex];

            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, JsonMemory.BufferSize - offsetInChunk);
            var chunkSpan = chunk.AsSpan(offsetInChunk, bytesToCopyFromThisChunk);

            var span = writer.GetSpan(chunkSpan.Length);
            chunkSpan.CopyTo(span);
            writer.Advance(chunkSpan.Length);
            bytesRead += bytesToCopyFromThisChunk;
            currentLocation += bytesToCopyFromThisChunk;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadRawValue(DbRow row, bool includeQuotes)
    {
        if (row.IsSimpleValue && includeQuotes && row.TokenType == JsonTokenType.String)
        {
            // Start one character earlier than the value (the open quote)
            // End one character after the value (the close quote)
            return ReadRawValue(row.Location - 1, row.SizeOrLength + 2);
        }

        return ReadRawValue(row.Location, row.SizeOrLength);
    }

    internal ReadOnlySpan<byte> ReadRawValue(int location, int size)
    {
        var startChunkIndex = location / JsonMemory.BufferSize;
        var offsetInStartChunk = location % JsonMemory.BufferSize;

        if (offsetInStartChunk + size <= JsonMemory.BufferSize)
        {
            return _dataChunks[startChunkIndex].AsSpan(offsetInStartChunk, size);
        }

        Span<byte> buffer = new byte[size];
        var bytesRead = 0;
        var currentLocation = location;

        while (bytesRead < size)
        {
            var chunkIndex = currentLocation / JsonMemory.BufferSize;
            var offsetInChunk = currentLocation % JsonMemory.BufferSize;
            var chunk = _dataChunks[chunkIndex];

            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, JsonMemory.BufferSize - offsetInChunk);
            var chunkSpan = chunk.AsSpan(offsetInChunk, bytesToCopyFromThisChunk);

            chunkSpan.CopyTo(buffer[bytesRead..]);
            bytesRead += bytesToCopyFromThisChunk;
            currentLocation += bytesToCopyFromThisChunk;
        }

        return buffer;
    }

    internal ReadOnlyMemory<byte> ReadRawValueAsMemory(int location, int size)
    {
        var startChunkIndex = location / JsonMemory.BufferSize;
        var offsetInStartChunk = location % JsonMemory.BufferSize;

        if (offsetInStartChunk + size <= JsonMemory.BufferSize)
        {
            return _dataChunks[startChunkIndex].AsMemory(offsetInStartChunk, size);
        }

        var tempArray = new byte[size];
        var bytesRead = 0;
        var currentLocation = location;

        while (bytesRead < size)
        {
            var chunkIndex = currentLocation / JsonMemory.BufferSize;
            var offsetInChunk = currentLocation % JsonMemory.BufferSize;
            var chunk = _dataChunks[chunkIndex];

            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, JsonMemory.BufferSize - offsetInChunk);

            chunk.AsSpan(offsetInChunk, bytesToCopyFromThisChunk)
                .CopyTo(tempArray.AsSpan(bytesRead));

            bytesRead += bytesToCopyFromThisChunk;
            currentLocation += bytesToCopyFromThisChunk;
        }

        return tempArray;
    }

    private static void CheckExpectedType(JsonTokenType expected, JsonTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException($"Expected {expected} but got {actual}.");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_pooledMemory)
            {
                JsonMemory.Return(JsonMemoryKind.Json, _dataChunks, _usedChunks);

                if (_dataChunks.Length > 1)
                {
                    _dataChunks.AsSpan(0, _usedChunks).Clear();
                    ArrayPool<byte[]>.Shared.Return(_dataChunks);
                }
            }

            _parsedData.Dispose();

            _disposed = true;
        }
    }
}
