namespace StrawberryShake.Serialization;

public class UnsignedLongSerializerTests
{
    private UnsignedLongSerializer Serializer { get; } = new();

    private UnsignedLongSerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        const ulong value = 4294967296;

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void Format_Null()
    {
        // arrange

        // act
        var result = Serializer.Format(null);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Format_Value()
    {
        // arrange
        const ulong value = 4294967296;

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void Format_Exception()
    {
        // arrange
        const string value = "4294967296";

        // act
        void Action() => Serializer.Format(value);

        // assert
        Assert.Equal(
            "SS0007",
            Assert.Throws<GraphQLClientException>(Action).Errors.Single().Code);
    }

    [Fact]
    public void TypeName_Default()
    {
        // arrange

        // act
        var typeName = Serializer.TypeName;

        // assert
        Assert.Equal("UnsignedLong", typeName);
    }

    [Fact]
    public void TypeName_Custom()
    {
        // arrange

        // act
        var typeName = CustomSerializer.TypeName;

        // assert
        Assert.Equal("Abc", typeName);
    }
}
