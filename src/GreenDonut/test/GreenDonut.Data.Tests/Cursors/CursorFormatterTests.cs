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
        Assert.Equal(@"{}test\:345:description\:123", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
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

    [Fact]
    public void Format_Nested_Key_With_Null_Intermediate_Yields_Null_Value()
    {
        // arrange
        var entity = new Parent { Child = null };
        Expression<Func<Parent, object?>> selector = x => x.Child!.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}\\null", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Nested_Key_With_NonNull_Intermediate()
    {
        // arrange
        var entity = new Parent { Child = new Child { Name = "test" } };
        Expression<Func<Parent, object?>> selector = x => x.Child!.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Nested_Key_Reads_Intermediate_Once()
    {
        // arrange
        var entity = new CountingParent { Child = new Child { Name = "test" } };
        Expression<Func<CountingParent, object?>> selector = x => x.Child!.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
        Assert.Equal(1, entity.ChildReads);
    }

    [Fact]
    public void Format_Deeply_Nested_Key_With_Null_Inner_Intermediate_Yields_Null_Value()
    {
        // arrange
        var entity = new GrandParent { Parent = new Parent { Child = null } };
        Expression<Func<GrandParent, object?>> selector = x => x.Parent!.Child!.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}\\null", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Deeply_Nested_Key_With_NonNull_Intermediates()
    {
        // arrange
        var entity = new GrandParent { Parent = new Parent { Child = new Child { Name = "test" } } };
        Expression<Func<GrandParent, object?>> selector = x => x.Parent!.Child!.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Nested_Key_Null_Check_Ignores_Overloaded_Equality()
    {
        // arrange
        // The null-safety must rely on a reference null check, not a user-defined
        // operator ==, so a null intermediate short-circuits no matter how the
        // type defines equality.
        var entity = new EqualityOverrideParent { Child = null };
        Expression<Func<EqualityOverrideParent, object?>> selector = x => x.Child!.Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}\\null", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Nested_Key_With_Null_Nullable_Value_Type_Intermediate_Yields_Null_Value()
    {
        // arrange
        var entity = new NullableStructParent { Moment = null };
        Expression<Func<NullableStructParent, object?>> selector = x => x.Moment!.Value.Hour;
        var serializer = new IntCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}\\null", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Nested_Key_With_Cast_Intermediate_Extracts_Value()
    {
        // arrange
        // A cast on the access path must be preserved so the leaf member binds
        // against the cast type, not the declared (base) type.
        var entity = new CastParent { Child = new SpecialChild { Name = "test" } };
        Expression<Func<CastParent, object?>> selector = x => ((SpecialChild)x.Child!).Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}test", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    [Fact]
    public void Format_Nested_Key_With_Cast_Intermediate_And_Null_Yields_Null_Value()
    {
        // arrange
        var entity = new CastParent { Child = null };
        Expression<Func<CastParent, object?>> selector = x => ((SpecialChild)x.Child!).Name;
        var serializer = new StringCursorKeySerializer();

        // act
        var result = CursorFormatter.Format(entity, [new CursorKey(selector, serializer)]);

        // assert
        Assert.Equal("{}\\null", Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    public class MyClass
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }

    public class GrandParent
    {
        public Parent? Parent { get; set; }
    }

    public class Parent
    {
        public Child? Child { get; set; }
    }

    public class CountingParent
    {
        public int ChildReads { get; private set; }

        public Child? Child
        {
            get
            {
                ChildReads++;
                return field;
            }
            set;
        }
    }

    public class Child
    {
        public string? Name { get; set; }
    }

    public class EqualityOverrideParent
    {
        public EqualityOverrideChild? Child { get; set; }
    }

    public class EqualityOverrideChild
    {
        public string? Name { get; set; }

        // Reports inequality even for two null references, so a guard relying on
        // this operator would fail to detect a null intermediate.
        public static bool operator ==(EqualityOverrideChild? left, EqualityOverrideChild? right) => false;

        public static bool operator !=(EqualityOverrideChild? left, EqualityOverrideChild? right) => true;

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => 0;
    }

    public class NullableStructParent
    {
        public DateTime? Moment { get; set; }
    }

    public class CastParent
    {
        public ChildBase? Child { get; set; }
    }

    public class ChildBase;

    public class SpecialChild : ChildBase
    {
        public string? Name { get; set; }
    }
}
