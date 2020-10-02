using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HotChocolate.Execution;
using HotChocolate.Stitching.Utilities;
using Snapshooter.Xunit;
using HotChocolate.Language;
using Snapshooter;
using System.Linq;
using HotChocolate.Stitching.Pipeline;
using Newtonsoft.Json.Linq;

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

            var result = QueryResultBuilder.New();
            var data = new OrderedDictionary();
            data["foo"] = objectList;
            data["bar"] = scalarList;
            data["baz"] = baz;
            result.SetData(data);

            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result.Create(), stream);
            byte[] buffer = stream.ToArray();

            var serializedResult = Utf8GraphQLRequestParser.ParseJson(buffer);

            // act
            IReadOnlyQueryResult deserializedResult =
                HttpResponseDeserializer.Deserialize(
                    (IReadOnlyDictionary<string, object>)serializedResult);

            // assert
            Snapshot.Match(deserializedResult,
                "DeserializeQueryResult_" + type);
        }

        [Fact]
        public async Task DeserializeQueryResultWithExtensions()
        {
            // arrange
            var qux = new OrderedDictionary { { "quux", 123 } };
            var baz = new OrderedDictionary { { "qux", qux } };
            var objectList = new List<object> { baz };
            var scalarList = new List<object> { 123 };

            var result = QueryResultBuilder.New();

            var data = new OrderedDictionary();
            data["foo"] = objectList;
            data["bar"] = scalarList;
            data["baz"] = baz;
            result.SetData(data);

            var extensionData = new ExtensionData();
            extensionData["foo"] = objectList;
            extensionData["bar"] = scalarList;
            extensionData["baz"] = baz;
            result.SetExtensions(extensionData);

            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result.Create(), stream);
            byte[] buffer = stream.ToArray();

            var serializedResult = Utf8GraphQLRequestParser.ParseJson(buffer);

            // act
            IReadOnlyQueryResult deserializedResult =
                HttpResponseDeserializer.Deserialize(
                    (IReadOnlyDictionary<string, object>)serializedResult);

            // assert
            deserializedResult.MatchSnapshot(m => m.Ignore(c => c.Field<JObject>("Extensions")));
            deserializedResult.Extensions.OrderBy(t => t.Key)
                .MatchSnapshot(new SnapshotNameExtension("extensions"));
        }

        [Fact]
        public async Task DeserializeQueryResultWithErrors()
        {
            // arrange
            var qux = new OrderedDictionary { { "quux", 123 } };
            var baz = new OrderedDictionary { { "qux", qux } };
            var objectList = new List<object> { baz };
            var scalarList = new List<object> { 123 };

            var result = QueryResultBuilder.New();

            var data = new OrderedDictionary();
            data["foo"] = objectList;
            data["bar"] = scalarList;
            data["baz"] = baz;
            result.SetData(data);

            result.AddError(ErrorBuilder.New()
                .SetMessage("foo")
                .SetPath(Path.New("root").Append("child"))
                .AddLocation(new Location(15, 16))
                .SetExtension("bar", "baz")
                .Build());

            result.AddError(ErrorBuilder.New()
                .SetMessage("qux")
                .SetExtension("bar", "baz")
                .Build());

            result.AddError(ErrorBuilder.New()
                .SetMessage("quux")
                .Build());

            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result.Create(), stream);
            byte[] buffer = stream.ToArray();

            var serializedResult = Utf8GraphQLRequestParser.ParseJson(buffer);

            // act
            IReadOnlyQueryResult deserializedResult =
                HttpResponseDeserializer.Deserialize(
                    (IReadOnlyDictionary<string, object>)serializedResult);

            // assert
            Snapshot.Match(deserializedResult);
        }

        [Fact]
        public async Task DeserializeQueryResultOnlyErrors()
        {
            // arrange
            var qux = new OrderedDictionary { { "quux", 123 } };
            var baz = new OrderedDictionary { { "qux", qux } };
            var objectList = new List<object> { baz };
            var scalarList = new List<object> { 123 };

            var result = QueryResultBuilder.New();

            result.AddError(ErrorBuilder.New()
                .SetMessage("foo")
                .SetPath(Path.New("root").Append("child"))
                .AddLocation(new Location(15, 16))
                .SetExtension("bar", "baz")
                .Build());

            result.AddError(ErrorBuilder.New()
                .SetMessage("qux")
                .SetExtension("bar", "baz")
                .Build());

            result.AddError(ErrorBuilder.New()
                .SetMessage("quux")
                .Build());


            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result.Create(), stream);
            byte[] buffer = stream.ToArray();

            var serializedResult = Utf8GraphQLRequestParser.ParseJson(buffer);

            // act
            IReadOnlyQueryResult deserializedResult =
                HttpResponseDeserializer.Deserialize(
                    (IReadOnlyDictionary<string, object>)serializedResult);

            // assert
            Snapshot.Match(deserializedResult);
        }
    }
}
