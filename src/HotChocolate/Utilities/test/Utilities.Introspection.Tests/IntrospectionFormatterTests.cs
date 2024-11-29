using System.Text.Json;
using Xunit;
using static HotChocolate.Utilities.Introspection.IntrospectionClient;

namespace HotChocolate.Utilities.Introspection;

public class IntrospectionFormatterTests
{
    [Fact]
    public void DeserializeStarWarsIntrospectionResult()
    {
        // arrange
        var json = FileResource.Open("StarWarsIntrospectionResult.json");
        var result = JsonSerializer.Deserialize<IntrospectionResult>(json, SerializerOptions);

        // act
        var schema = IntrospectionFormatter.Format(result!);

        // assert
        schema.ToString(true).MatchSnapshot();
    }

    [Fact]
    public void DeserializeIntrospectionWithIntDefaultValues()
    {
        // arrange
        var json = FileResource.Open("IntrospectionWithDefaultValues.json");
        var result = JsonSerializer.Deserialize<IntrospectionResult>(json, SerializerOptions);

        // act
        var schema = IntrospectionFormatter.Format(result!);

        // assert
        schema.ToString(true).MatchSnapshot();
    }
}
