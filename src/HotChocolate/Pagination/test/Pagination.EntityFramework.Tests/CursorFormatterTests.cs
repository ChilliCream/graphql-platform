using System.Linq.Expressions;
using System.Text;
using System.Text.Unicode;
using HotChocolate.Pagination.Expressions;
using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Data;

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
        Assert.Equal("dGVzdA==", result);
        Assert.Equal("test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
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
        Assert.Equal("dGVzdFw6dGVzdA==", result);
        Assert.Equal("test\\:test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
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
        Assert.Equal("dGVzdDpkZXNjcmlwdGlvbg==", result);
        Assert.Equal("test:description", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
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
        Assert.Equal("dGVzdFw6MzQ1OmRlc2NyaXB0aW9uXDoxMjM=", result);
        Assert.Equal("test\\:345:description\\:123", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
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
        var parsed =CursorParser.Parse(
            formatted,
            [
                new CursorKey(selector1, serializer),
                new CursorKey(selector2, serializer)
            ]);

        // assert
        Assert.Equal("test:345", parsed[0]);
        Assert.Equal("description:123", parsed[1]);
    }

    public class MyClass
    {
        public string Name { get; set; } = default!;

        public string? Description { get; set; } = default!;
    }
}
