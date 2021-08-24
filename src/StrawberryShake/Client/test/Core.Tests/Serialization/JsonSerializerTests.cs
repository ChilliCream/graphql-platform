using System.Linq;
using System.Text.Json;
using Xunit;

namespace StrawberryShake.Serialization
{
    public class JsonSerializerTests
    {
        private readonly JsonSerializer _serializer = new();

        [Fact]
        public void Parse()
        {
            // arrange
            var json = JsonDocument.Parse(@"{ ""abc"": { ""def"": ""def"" } }");
            JsonElement element = json.RootElement.EnumerateObject().First().Value;

            // act
            JsonDocument document = _serializer.Parse(element);

            // assert
            Assert.Equal(element.GetRawText(), document.RootElement.GetRawText());
        }

        [Fact]
        public void Format()
        {
            // arrange
            var json = JsonDocument.Parse(@"{ ""abc"": { ""def"": ""def"" } }");

            // act
            var result = _serializer.Format(json);

            // assert
            Assert.Equal(json.RootElement, Assert.IsType<JsonElement>(result));
        }
    }
}
