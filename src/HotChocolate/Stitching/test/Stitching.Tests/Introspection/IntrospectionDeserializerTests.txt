using System;
using ChilliCream.Testing;
using HotChocolate.Language;
using Snapshooter.Xunit;
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
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void JsonIsNull()
        {
            // arrange
            // act
            Action action = () => IntrospectionDeserializer.Deserialize(null);

            // assert
            Assert.Throws<ArgumentException>(action).Message.MatchSnapshot();
        }

        [Fact]
        public void DeserializeIntrospectionWithIntDefaultValues()
        {
            // arrange
            string json = FileResource.Open("IntrospectionWithDefaultValues.json");

            // act
            DocumentNode schema = IntrospectionDeserializer.Deserialize(json);

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }
    }
}
