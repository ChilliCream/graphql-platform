using System.Linq.Expressions;
using System.Text;
using GreenDonut.Data.Cursors.Serializers;

namespace GreenDonut.Data.Cursors;

public class CursorFormatterTests
{
    [Fact]
    public void Format_Single_Key()
    {
        // arrange
        var entity = new MyClass { Name = "test" };
        Expression<Func<MyClass, object>> selector = x => x.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("e310ZXN0", result);
        Assert.Equal("{}test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Single_Key_With_Colon()
    {
        // arrange
        var entity = new MyClass { Name = "test:test" };
        Expression<Func<MyClass, object>> selector = x => x.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("e310ZXN0XDp0ZXN0", result);
        Assert.Equal("{}test\\:test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Two_Keys()
    {
        // arrange
        var entity = new MyClass { Name = "test", Description = "description" };
        Expression<Func<MyClass, object?>> selector1 = x => x.Name;
        Expression<Func<MyClass, object?>> selector2 = x => x.Description;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(
            entity,
            [
                new CursorKey(selector1, serializer),
                new CursorKey(selector2, serializer)
            ]);

        // assert
        Assert.Equal("e310ZXN0OmRlc2NyaXB0aW9u", result);
        Assert.Equal("{}test:description", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Two_Keys_With_Colon()
    {
        // arrange
        var entity = new MyClass { Name = "test:345", Description = "description:123" };
        Expression<Func<MyClass, object?>> selector1 = x => x.Name;
        Expression<Func<MyClass, object?>> selector2 = x => x.Description;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(
            entity,
            [
                new CursorKey(selector1, serializer),
                new CursorKey(selector2, serializer)
            ]);

        // assert
        Assert.Equal("e310ZXN0XDozNDU6ZGVzY3JpcHRpb25cOjEyMw==", result);
        Assert.Equal("{}test\\:345:description\\:123", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_And_Parse_Two_Keys_With_Colon()
    {
        // arrange
        var entity = new MyClass { Name = "test:345", Description = "description:123" };
        Expression<Func<MyClass, object?>> selector1 = x => x.Name;
        Expression<Func<MyClass, object?>> selector2 = x => x.Description;
        var serializer = new StringCursorKeySerializer();

        // act
        var formatted = CursorFormatter.Format(
            entity,
            [
                new CursorKey(selector1, serializer),
                new CursorKey(selector2, serializer)
            ]);
        var parsed = CursorParser.Parse(
            formatted,
            [
                new CursorKey(selector1, serializer),
                new CursorKey(selector2, serializer)
            ]);

        // assert
        Assert.Null(parsed.Offset);
        Assert.Null(parsed.PageIndex);
        Assert.Null(parsed.TotalCount);
        Assert.Equal("test:345", parsed.Values[0]);
        Assert.Equal("description:123", parsed.Values[1]);
    }

    public class MyClass
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}
