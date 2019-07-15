using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Subscriptions
{
    public class JsonPayloadSerializerTests
    {
        [Fact]
        public async Task GivenSerializer_WhenSerialize_ContentIsValid()
        {
            // arrange
            var serializer = new JsonPayloadSerializer();
            var payload = "Foo";

            // act
            var encoded = await serializer.SerializeAsync(payload);

            // assert
            var expected = new byte[] { 34, 70, 111, 111, 34 };
            Assert.Equal(expected, encoded);
        }

        [Fact]
        public async Task GivenSerializer_WhenDeserialize_ContentIsValid()
        {
            // arrange
            var serializer = new JsonPayloadSerializer();
            var content = new byte[] { 34, 70, 111, 111, 34 };

            // act
            var decoded = await serializer.DeserializeAsync(content);

            // assert
            var expected = "Foo";
            Assert.Equal(expected, decoded);
        }
    }
}
