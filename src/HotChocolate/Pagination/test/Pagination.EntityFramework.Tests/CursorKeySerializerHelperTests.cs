using System.Text;
using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Data;

public static class CursorKeySerializerHelperTests
{
    private static readonly ICursorKeySerializer _serializer = new StringCursorKeySerializer();

    [Fact]
    public static void Parse_Null_ReturnsNull()
    {
        // arrange
        var formattedKey = CursorKeySerializerHelper.Null;

        // act
        var result = CursorKeySerializerHelper.Parse(formattedKey, _serializer);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public static void Parse_EscapedNull_ReturnsEscapedNull()
    {
        // arrange
        var formattedKey = CursorKeySerializerHelper.EscapedNull;

        // act
        var result = CursorKeySerializerHelper.Parse(formattedKey, _serializer);

        // assert
        Assert.Equal("\\null", result);
    }

    [Fact]
    public static void Parse_NoColons_ReturnsParsedValue()
    {
        // arrange
        var formattedKey = "testvalue"u8.ToArray();
        var expectedValue = "testvalue";

        // act
        var result = CursorKeySerializerHelper.Parse(formattedKey, _serializer);

        // assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public static void Parse_WithEscapedColons_ReturnsParsedValue()
    {
        // arrange
        var formattedKey = "part1\\:part2"u8.ToArray();
        var expectedValue = "part1:part2";

        // act
        var result = CursorKeySerializerHelper.Parse(formattedKey, _serializer);

        // assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public static void TryFormat_NullKey_FormatsToNull()
    {
        // arrange
        object? key = null;
        var buffer = new byte[CursorKeySerializerHelper.Null.Length];

        // act
        var success = CursorKeySerializerHelper.TryFormat(key, _serializer, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(CursorKeySerializerHelper.Null.Length, written);
        Assert.True(CursorKeySerializerHelper.Null.SequenceEqual(buffer));
    }

    [Fact]
    public static void TryFormat_EscapedNull_FormatsToEscapedNull()
    {
        // arrange
        object key = "\\null";
        var buffer = new byte[CursorKeySerializerHelper.EscapedNull.Length];

        // act
        var success = CursorKeySerializerHelper.TryFormat(key, _serializer, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(CursorKeySerializerHelper.EscapedNull.Length, written);
        Assert.True(CursorKeySerializerHelper.EscapedNull.SequenceEqual(buffer));
    }

    [Fact]
    public static void TryFormat_StringWithoutColons_FormatsCorrectly()
    {
        // arrange
        object key = "testvalue";
        Span<byte> buffer = new byte[10];

        // act
        var success = CursorKeySerializerHelper.TryFormat(key, _serializer, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(9, written); // "testvalue" is 9 bytes
        Assert.Equal("testvalue", Encoding.UTF8.GetString(buffer.Slice(0, written)));
    }

    [Fact]
    public static void TryFormat_StringWithColons_EscapesAndFormatsCorrectly()
    {
        // arrange
        object key = "part1:part2";
        Span<byte> buffer = new byte[12]; // "part1\\:part2" is 12 bytes

        // act
        var success = CursorKeySerializerHelper.TryFormat(key, _serializer, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(12, written); // "part1\\:part2" is 12 bytes
        Assert.Equal("part1\\:part2", Encoding.UTF8.GetString(buffer.Slice(0, written)));
    }

    [Fact]
    public static void TryFormat_BufferTooSmall_ReturnsFalse()
    {
        // arrange
        object key = "part1:part2";
        var buffer = new byte[10]; // Too small for "part1\\:part2"

        // act
        var success = CursorKeySerializerHelper.TryFormat(key, _serializer, buffer, out var written);

        // assert
        Assert.False(success);
        Assert.Equal(0, written);
    }
}
