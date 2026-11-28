using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Text.Json;

public class ResultDocumentValueTests
{
    [Fact]
    public void SetStringValue_Should_RoundTripBytes_When_WritingSingleMultiMegabyteValue()
    {
        // arrange
        var expected = new byte[5 * 1024 * 1024];
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        for (var i = 0; i < expected.Length; i++)
        {
            expected[i] = (byte)('a' + (i % 26));
        }

        // act
        resultValue.SetStringValue(expected);
        var actual = Encoding.UTF8.GetBytes(resultValue.GetString()!);

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("plain ascii text")]
    [InlineData("quote \" backslash \\ and a line\nbreak")]
    [InlineData("\u00e4\u00f6\u00fc \u20ac \u4e2d\u6587")]
    [InlineData("surrogate pair \ud83d\ude00 in the middle")]
    public void SetStringValue_Should_RoundTripText_When_ValueIsUtf16(string expected)
    {
        // arrange
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        // act
        resultValue.SetStringValue(expected.AsSpan());
        var actual = resultValue.GetString();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetStringValue_Should_ReplaceLoneSurrogate_When_ValueIsInvalidUtf16()
    {
        // arrange
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        // act
        resultValue.SetStringValue("a\ud83db".AsSpan());
        var actual = resultValue.GetString();

        // assert
        Assert.Equal("a\ufffdb", actual);
    }

    [Fact]
    public void SetStringValue_Should_WriteReplacementCharacter_When_SurrogatesAreUnpaired()
    {
        // arrange
        // a single high surrogate, a low surrogate without a high one, a high surrogate at the end, a reversed pair
        string[] values = ["\ud83d", "a\ude00b", "ab\ud83d", "\ude00\ud83d"];
        string[] expected = ["EFBFBD", "61EFBFBD62", "6162EFBFBD", "EFBFBDEFBFBD"];
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        var actual = new string[values.Length];

        // act
        resultValue.SetArrayValue(values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            resultValue[i].SetStringValue(values[i].AsSpan());
        }

        for (var i = 0; i < values.Length; i++)
        {
            actual[i] = Convert.ToHexString(resultValue[i].GetRawValue(includeQuotes: false));
        }

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetStringValue_Should_WriteReplacementCharacter_When_InvalidValuesCrossDataChunkBoundaries()
    {
        // arrange
        // 300 values of 97 bytes each cross the boundaries of the first data chunks at varying offsets
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        var values = new string[300];
        var expected = new string[values.Length];
        var actual = new string?[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var start = i.ToString("D3") + new string((char)('a' + (i % 26)), 30);
            values[i] = start + "\ud83d" + new string('z', 61);
            expected[i] = start + "�" + new string('z', 61);
        }

        // act
        resultValue.SetArrayValue(values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            resultValue[i].SetStringValue(values[i].AsSpan());
        }

        for (var i = 0; i < values.Length; i++)
        {
            actual[i] = resultValue[i].GetString();
        }

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetStringValue_Should_RoundTripText_When_ValuesCrossDataChunkBoundaries()
    {
        // arrange
        // 300 values of 97 bytes each cross the boundaries of the first data chunks at varying offsets
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        var expected = new string[300];
        var actual = new string?[expected.Length];

        for (var i = 0; i < expected.Length; i++)
        {
            expected[i] = i.ToString("D3") + new string((char)('a' + (i % 26)), 30) + "\u20ac" + new string('z', 61);
        }

        // act
        resultValue.SetArrayValue(expected.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            resultValue[i].SetStringValue(expected[i].AsSpan());
        }

        for (var i = 0; i < expected.Length; i++)
        {
            actual[i] = resultValue[i].GetString();
        }

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetStringValue_Should_KeepPreviousDataChunkTable_When_DataChunkTableGrows()
    {
        // arrange
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        var previous = GetData(resultDocument);

        // act
        resultValue.SetStringValue(new byte[5 * 1024 * 1024]);

        // assert
        // the shared pool serves a rent from the array this thread returned last
        var rented = ArrayPool<MemorySegment>.Shared.Rent(previous.Length);
        ArrayPool<MemorySegment>.Shared.Return(rented);
        Assert.NotSame(previous, GetData(resultDocument));
        Assert.NotSame(previous, rented);
    }

    [Fact]
    public void SetArrayValue_Should_KeepPreviousRowChunkTables_When_RowChunkTableGrowsTwice()
    {
        // arrange
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        var previous = GetChunks(ref resultDocument._metaDb);
        var firstChunk = previous[0].Buffer;

        // act
        resultValue.SetArrayValue(previous.Length * 2 * (128 * 1024 / ResultDocument.DbRow.Size));

        // assert
        Assert.True(GetChunks(ref resultDocument._metaDb).Length > previous.Length * 2);
        Assert.Same(firstChunk, previous[0].Buffer);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_data")]
    private static extern ref MemorySegment[] GetData(ResultDocument resultDocument);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_chunks")]
    private static extern ref MemorySegment[] GetChunks(ref ResultDocument.MetaDb metaDb);
}
