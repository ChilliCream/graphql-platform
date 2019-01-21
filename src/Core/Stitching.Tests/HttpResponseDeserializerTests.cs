using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HotChocolate.Stitching
{
    public class HttpResponseDeserializerTests
    {
        [InlineData("hello", "string")]
        [InlineData(1.5, "flot")]
        [InlineData(123, "int")]
        [InlineData(true, "bool")]
        [Theory]
        public async Task DeserializeQueryResult(object value, string type)
        {
            // arrange
            var qux = new OrderedDictionary { { "quux", value } };
            var baz = new OrderedDictionary { { "qux", qux } };
            var objectList = new List<object> { baz };
            var scalarList = new List<object> { value };
            var result = new QueryResult();
            result.Data["foo"] = objectList;
            result.Data["bar"] = scalarList;
            result.Data["baz"] = baz;

            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result, stream);
            byte[] buffer = stream.ToArray();

            string json = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var serializedResult = JsonConvert.DeserializeObject<JObject>(json);

            // act
            IReadOnlyQueryResult deserializedResult =
                HttpResponseDeserializer.Deserialize(serializedResult);

            // assert
            deserializedResult.Snapshot("DeserializeQueryResult_" + type);
        }
    }
}
