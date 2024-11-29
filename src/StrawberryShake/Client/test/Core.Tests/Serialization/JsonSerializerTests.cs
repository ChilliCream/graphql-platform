using System.Text.Json;

namespace StrawberryShake.Serialization;

public class JsonSerializerTests
{
    private readonly JsonSerializer _serializer = new();

    [Fact]
    public void Parse()
    {
        // arrange
        var json = JsonDocument.Parse(@"{ ""abc"": {""def"":""def""} }");
        var element = json.RootElement.EnumerateObject().First().Value;

        // act
        var serialized = _serializer.Parse(element);

        // assert
        Assert.Equal(element.ToString(), serialized.ToString());
    }

    [Fact]
    public void ParseLarge()
    {
        // arrange
        var json = System.Text.Json.JsonSerializer.SerializeToElement($@"""{"padding",10000}""");

        // act
        var serialized = _serializer.Parse(json);

        // assert
        Assert.Equal(json.ToString(), serialized.ToString());
    }

    [Fact]
    public void UseAfterDispose()
    {
        // arrange
        var json = JsonDocument.Parse(@"{ ""abc"": {""def"":""def""} }");
        var element = json.RootElement.EnumerateObject().First().Value;

        // act
        var serialized = _serializer.Parse(element);
        var expected = element.ToString();
        json.Dispose();

        // assert
        Assert.Equal(expected, serialized.ToString());
    }

    [Fact]
    public void Format()
    {
        // arrange
        using var json = JsonDocument.Parse(@"{ ""abc"": { ""def"": ""def"" } }");

        // act
        var result = _serializer.Format(json.RootElement);

        // assert
        Assert.Equal(json.RootElement, Assert.IsType<JsonElement>(result));
    }
}
