using System;
using StrawberryShake;
using StrawberryShake.Serialization;
using Xunit;

namespace Core.Tests.Serialization
{
    public class UuidSerializerTests
    {
        [Theory]
        [InlineData("D")]
        [InlineData("N")]
        [InlineData("B")]
        [InlineData("P")]
        [InlineData("X")]
        public void Parse_Different_Formats(string format)
        {
            // arrange
            var serializer = new UuidSerializer();
            var guid = Guid.NewGuid();

            // act
            Guid result = serializer.Parse(guid.ToString(format));

            // assert
            Assert.Equal(guid, result);
        }

        [Fact]
        public void Parse_Exception()
        {
            // arrange
            var serializer = new UuidSerializer();

            // assert
            Assert.Throws<GraphQLClientException>(() => serializer.Parse(string.Empty));
        }

        [Theory]
        [InlineData("D")]
        [InlineData("N")]
        [InlineData("B")]
        [InlineData("P")]
        [InlineData("X")]
        public void Format(string format)
        {
            // arrange
            var serializer = new UuidSerializer(format: format);
            var guid = Guid.NewGuid();

            // act
            var result = serializer.Format(guid);

            // assert
            Assert.Equal(guid.ToString(format), result);
        }
    }
}
