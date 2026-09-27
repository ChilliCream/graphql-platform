using System.Linq.Expressions;
using GreenDonut.Data.Cursors.Serializers;

namespace GreenDonut.Data.Cursors;

public class CursorParserTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 25)]
    [InlineData(-1, 0)]
    [InlineData(-1, 1)]
    [InlineData(-1, 25)]
    [InlineData(-5, 0)]
    [InlineData(-5, 1)]
    [InlineData(-5, 25)]
    public void Parse_RoundTrips_EndCursor(int offset, int totalCount)
    {
        // arrange
        var keys = CreateKeys();
        var formatted = CursorFormatter.FormatEndCursor(offset, totalCount);

        // act
        var parsed = CursorParser.Parse(formatted, keys);

        // assert
        Assert.True(parsed.IsEndCursor);
        Assert.False(parsed.IsRelative);
        Assert.Empty(parsed.Values);
        Assert.Equal(offset, parsed.Offset);
        Assert.Null(parsed.PageIndex);
        Assert.Equal(totalCount, parsed.TotalCount);
    }

    [Fact]
    public void Parse_Rejects_EndCursor_With_Unparsable_Body()
    {
        // arrange
        var keys = CreateKeys();
        var cursor = Convert.ToBase64String("{end|x}"u8);

        // act
        void Act() => CursorParser.Parse(cursor, keys);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The cursor page info could not be parsed.", exception.Message);
    }

    [Fact]
    public void Parse_Rejects_EndCursor_With_Positive_Offset()
    {
        // arrange
        var keys = CreateKeys();
        var cursor = Convert.ToBase64String("{end|1|5}"u8);

        // act
        void Act() => CursorParser.Parse(cursor, keys);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The cursor page info could not be parsed.", exception.Message);
    }

    [Fact]
    public void Parse_Rejects_EndCursor_With_Key_Values()
    {
        // arrange
        var keys = CreateKeys();
        var cursor = Convert.ToBase64String("{end|0|25}test"u8);

        // act
        void Act() => CursorParser.Parse(cursor, keys);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The cursor page info could not be parsed.", exception.Message);
    }

    [Fact]
    public void Parse_Parses_Legacy_ThreeNumber_PageInfo()
    {
        // arrange
        var entity = new MyClass { Name = "test" };
        var keys = CreateKeys();
        var formatted = CursorFormatter.Format(entity, keys, new CursorPageInfo(2, 3, 10));

        // act
        var parsed = CursorParser.Parse(formatted, keys);

        // assert
        Assert.False(parsed.IsEndCursor);
        Assert.True(parsed.IsRelative);
        Assert.Equal(2, parsed.Offset);
        Assert.Equal(3, parsed.PageIndex);
        Assert.Equal(10, parsed.TotalCount);
        Assert.Equal("test", parsed.Values[0]);
    }

    [Fact]
    public void Parse_Parses_Empty_PageInfo()
    {
        // arrange
        var entity = new MyClass { Name = "test" };
        var keys = CreateKeys();
        var formatted = CursorFormatter.Format(entity, keys);

        // act
        var parsed = CursorParser.Parse(formatted, keys);

        // assert
        Assert.False(parsed.IsEndCursor);
        Assert.False(parsed.IsRelative);
        Assert.Null(parsed.Offset);
        Assert.Null(parsed.PageIndex);
        Assert.Null(parsed.TotalCount);
        Assert.Equal("test", parsed.Values[0]);
    }

    private static CursorKey[] CreateKeys()
    {
        Expression<Func<MyClass, object?>> selector = x => x.Name;
        return [new CursorKey(selector, new StringCursorKeySerializer())];
    }

    public class MyClass
    {
        public string Name { get; set; } = null!;
    }
}
