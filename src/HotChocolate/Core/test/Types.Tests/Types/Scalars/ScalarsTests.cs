namespace HotChocolate.Types;

public class ScalarsTests
{
    [InlineData(Foo.Bar, ValueKind.Enum)]
    [InlineData("foo", ValueKind.String)]
    [InlineData((short)1, ValueKind.Integer)]
    [InlineData((int)1, ValueKind.Integer)]
    [InlineData((long)1, ValueKind.Integer)]
    [InlineData((ushort)1, ValueKind.Integer)]
    [InlineData((uint)1, ValueKind.Integer)]
    [InlineData((ulong)1, ValueKind.Integer)]
    [InlineData((float)1, ValueKind.Float)]
    [InlineData((double)1, ValueKind.Float)]
    [InlineData(null, ValueKind.Null)]
    [Theory]
    public void TryGetKind(object value, ValueKind expectedKind)
    {
        // arrange
        // act
        var isScalar = Scalars.TryGetKind(value, out var kind);

        // assert
        Assert.True(isScalar);
        Assert.Equal(expectedKind, kind);
    }

    [InlineData(Foo.Bar, ValueKind.Enum)]
    [InlineData((short)1, ValueKind.Integer)]
    [InlineData((int)1, ValueKind.Integer)]
    [InlineData((long)1, ValueKind.Integer)]
    [InlineData((ushort)1, ValueKind.Integer)]
    [InlineData((uint)1, ValueKind.Integer)]
    [InlineData((ulong)1, ValueKind.Integer)]
    [InlineData((float)1, ValueKind.Float)]
    [InlineData((double)1, ValueKind.Float)]
    [Theory]
    public void TryGetKind_From_Nullable(
        object value,
        ValueKind expectedKind)
    {
        // arrange
        var type = typeof(Nullable<>).MakeGenericType(value.GetType());
        var constructor =
            type.GetConstructor([value.GetType(),]);
        var nullableValue = constructor!.Invoke([value,]);

        // act
        var isScalar = Scalars.TryGetKind(
            nullableValue, out var kind);

        // assert
        Assert.True(isScalar);
        Assert.Equal(expectedKind, kind);
    }

    [Fact]
    public void Decimal_Is_Float()
    {
        // arrange
        var d = 123.123M;

        // act
        var isScalar = Scalars.TryGetKind(d, out var kind);

        // assert
        Assert.True(isScalar);
        Assert.Equal(ValueKind.Float, kind);
    }

    [Fact]
    public void NullableDecimal_Is_Float()
    {
        // arrange
        decimal? d = 123.123M;

        // act
        var isScalar = Scalars.TryGetKind(d, out var kind);

        // assert
        Assert.True(isScalar);
        Assert.Equal(ValueKind.Float, kind);
    }

    [Fact]
    public void Object_Is_Not_A_Serialized_Scalar()
    {
        // arrange
        var o = new object();

        // act
        var isScalar = Scalars.TryGetKind(o, out _);

        // assert
        Assert.False(isScalar);
    }

    [Fact]
    public void List_From_ListOfObject()
    {
        // arrange
        var list = new List<object>();

        // act
        var success = Scalars.TryGetKind(list, out var kind);

        // assert
        Assert.True(success);
        Assert.Equal(ValueKind.List, kind);
    }

    [Fact]
    public void List_From_ArrayOfObject()
    {
        // arrange
        var list = new object[0];

        // act
        var success = Scalars.TryGetKind(list, out var kind);

        // assert
        Assert.True(success);
        Assert.Equal(ValueKind.List, kind);
    }

    [Fact]
    public void Object_From_Dictionary()
    {
        // arrange
        var list = new Dictionary<string, object>();

        // act
        var success = Scalars.TryGetKind(list, out var kind);

        // assert
        Assert.True(success);
        Assert.Equal(ValueKind.Object, kind);
    }

    public enum Foo
    {
        Bar,
    }
}
