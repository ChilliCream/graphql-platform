namespace StrawberryShake.Serialization;

public class UriSerializerTests
{
    private UriSerializer Serializer { get; } = new();
    private UriSerializer CustomSerializer { get; } = new("Abc");

    [Theory]
    [InlineData("example.com")]
    [InlineData("https://example.com/")]
    public void Parse(string uri)
    {
        // arrange & act
        var result = Serializer.Parse(uri);

        // assert
        Assert.IsType<Uri>(result);
    }

    [Fact]
    public void TypeName_Default()
    {
        // arrange & act
        var typeName = Serializer.TypeName;

        // assert
        Assert.Equal("URI", typeName);
    }

    [Fact]
    public void TypeName_Custom()
    {
        // arrange & act
        var typeName = CustomSerializer.TypeName;

        // assert
        Assert.Equal("Abc", typeName);
    }
}
