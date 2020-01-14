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
            string json = FileResource.Open("StarWarsIntrospectionResult.json");
            IntrospectionResult result = JsonSerializer.Deserialize<IntrospectionResult>(
                json,
                IntrospectionClient.SerializerOptions);

            // act
            DocumentNode schema = IntrospectionDeserializer.Deserialize(result);

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void DeserializeIntrospectionWithIntDefaultValues()
        {
            // arrange
            string json = FileResource.Open("IntrospectionWithDefaultValues.json");
            IntrospectionResult result = JsonSerializer.Deserialize<IntrospectionResult>(
                json,
                IntrospectionClient.SerializerOptions);

            // act
            DocumentNode schema = IntrospectionDeserializer.Deserialize(result);

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }
    }
}
