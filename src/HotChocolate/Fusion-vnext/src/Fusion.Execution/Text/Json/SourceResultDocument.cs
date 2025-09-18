using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private MetaDb _parsedData;
    private byte[][] _dataChunks;
    private bool _disposed;

    private SourceResultDocument(MetaDb parsedData, byte[][] dataChunks)
    {
        _parsedData = parsedData;
        _dataChunks = dataChunks;
    }

    internal int Id;

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

    private ReadOnlySpan<byte> ReadRawValue(DbRow row)
        => throw new NotImplementedException();

    private static void CheckExpectedType(JsonTokenType expected, JsonTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}
