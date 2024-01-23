namespace StrawberryShake.Serialization;

public class ByteArraySerializerTests
{
    private ByteArraySerializer Serializer { get; } = new();

    private ByteArraySerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        byte[] buffer = [1,];

        // act
        var result = Serializer.Parse(buffer);

        // assert
        Assert.Same(buffer, result);
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
        byte[] buffer = [1,];

        // act
        var result = Serializer.Format(buffer);

        // assert
        Assert.Same(buffer, result);
    }

    [Fact]
    public void TypeName_Default()
    {
        // arrange

        // act
        var typeName = Serializer.TypeName;

        // assert
        Assert.Equal("ByteArray", typeName);
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
