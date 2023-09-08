using System.Text.Json;
using ChilliCream.Testing;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities.Introspection
{
    public class IntrospectionDeserializerTests
    {
        [Fact]
        public void DeserializeStarWarsIntrospectionResult()
        {
            // arrange
            var json = FileResource.Open("StarWarsIntrospectionResult.json");
            var result = JsonSerializer.Deserialize<IntrospectionResult>(
                json,
                IntrospectionClient.SerializerOptions);

            // act
            var schema = IntrospectionDeserializer.Deserialize(result);

            // assert
            schema.ToString(true).MatchSnapshot();
        }

        [Fact]
        public void DeserializeIntrospectionWithIntDefaultValues()
        {
            // arrange
            var json = FileResource.Open("IntrospectionWithDefaultValues.json");
            var result = JsonSerializer.Deserialize<IntrospectionResult>(
                json,
                IntrospectionClient.SerializerOptions);

            // act
            var schema = IntrospectionDeserializer.Deserialize(result);

            // assert
            schema.ToString(true).MatchSnapshot();
        }
    }
}
