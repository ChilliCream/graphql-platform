namespace StrawberryShake.Serialization;

public class UnsignedByteSerializerTests
{
    private UnsignedByteSerializer Serializer { get; } = new();

    private UnsignedByteSerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        const byte value = 1;

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
        const byte value = 1;

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void Format_Exception()
    {
        // arrange
        const string value = "1";

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
        Assert.Equal("UnsignedByte", typeName);
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
