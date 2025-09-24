using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private MetaDb _parsedData;
    private readonly byte[][] _dataChunks;
    private readonly bool _pooledMemory;
    private bool _disposed;

    private SourceResultDocument(MetaDb parsedData, byte[][] dataChunks, bool pooledMemory)
    {
        _parsedData = parsedData;
        _dataChunks = dataChunks;
        _pooledMemory = pooledMemory;
        Root = new SourceResultElement(this, 0);
    }

    internal int Id { get; set; } = -1;

    public SourceResultElement Root { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal JsonTokenType GetElementTokenType(int index)
        => _parsedData.GetJsonTokenType(index);

    internal int GetArrayLength(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.StartArray, row.TokenType);

        return row.SizeOrLength;
    }

    internal int GetPropertyCount(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.StartObject, row.TokenType);

        return row.SizeOrLength;
    }

    internal SourceResultElement GetArrayIndexElement(int currentIndex, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(currentIndex);

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
            return new SourceResultElement(this, currentIndex + ((arrayIndex + 1) * DbRow.Size));
        }

        var elementCount = 0;
        var objectOffset = currentIndex + DbRow.Size;

        for (; objectOffset < _parsedData.Length; objectOffset += DbRow.Size)
        {
            if (arrayIndex == elementCount)
            {
                return new SourceResultElement(this, objectOffset);
            }

            row = _parsedData.Get(objectOffset);

            if (!row.IsSimpleValue)
            {
                objectOffset += DbRow.Size * row.NumberOfRows;
            }

            elementCount++;
        }

        Debug.Fail(
            "Ran out of database searching for array index "
            + $"{arrayIndex} from {currentIndex} when length was {arrayLength}");
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadRawValue(DbRow row)
        => ReadRawValue(row.Location, row.SizeOrLength);

    private ReadOnlySpan<byte> ReadRawValue(int location, int size)
    {
        const int chunkSize = 128 * 1024;

        // Calculate which chunk contains the start of our data
        var startChunkIndex = location / chunkSize;
        var offsetInStartChunk = location % chunkSize;

        // Fast path: Value fits entirely within one chunk
        if (offsetInStartChunk + size <= chunkSize)
        {
            return _dataChunks[startChunkIndex].AsSpan(offsetInStartChunk, size);
        }

        // TODO : we need to use pooled memory in this case.
        // TODO : also we should measure how often we end up here
        // Slow path: Value spans across multiple chunks - create temporary array
        var tempArray = new byte[size];
        var bytesRead = 0;
        var currentLocation = location;

        while (bytesRead < size)
        {
            var chunkIndex = currentLocation / chunkSize;
            var offsetInChunk = currentLocation % chunkSize;
            var chunk = _dataChunks[chunkIndex];

            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, chunkSize - offsetInChunk);

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
            throw new ArgumentOutOfRangeException();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_pooledMemory)
            {
                JsonMemory.Return(_dataChunks);
            }

            _disposed = true;
        }
    }
}
