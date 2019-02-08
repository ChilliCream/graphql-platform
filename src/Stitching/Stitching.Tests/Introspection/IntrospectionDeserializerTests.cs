using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching.Introspection
{
    public class IntrospectionDeserializerTests
    {
        [Fact]
        public void DeserializeStarWarsIntrospectionResult()
        {
            // arrange
            string json = FileResource.Open("StarWarsIntrospectionResult.json");

            // act
            DocumentNode schema = IntrospectionDeserializer.Deserialize(json);

            // assert
            SchemaSyntaxSerializer.Serialize(schema).Snapshot();
        }
    }
}
