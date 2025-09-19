namespace StrawberryShake.Serialization;

public class UrlSerializerTests
{
    private UrlSerializer Serializer { get; } = new();
    private UrlSerializer CustomSerializer { get; } = new("Abc");

    [Theory]
    [InlineData("example.com")]
    [InlineData("https://example.com/")]
    public void Parse(string url)
    {
        // arrange & act
        var result = Serializer.Parse(url);

        // assert
        Assert.IsType<Uri>(result);
    }

    [Fact]
    public void TypeName_Default()
    {
        // arrange & act
        var typeName = Serializer.TypeName;

        // assert
        Assert.Equal("Url", typeName);
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
