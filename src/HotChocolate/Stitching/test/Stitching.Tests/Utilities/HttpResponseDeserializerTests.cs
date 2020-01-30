using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HotChocolate.Execution;
using HotChocolate.Stitching.Utilities;
using Snapshooter.Xunit;
using HotChocolate.Language;

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

            var result = new QueryResult();

            result.Data["foo"] = objectList;
            result.Data["bar"] = scalarList;
            result.Data["baz"] = baz;

            result.Extensions["foo"] = objectList;
            result.Extensions["bar"] = scalarList;
            result.Extensions["baz"] = baz;

            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result, stream);
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
        public async Task DeserializeQueryResultWithErrors()
        {
            // arrange
            var qux = new OrderedDictionary { { "quux", 123 } };
            var baz = new OrderedDictionary { { "qux", qux } };
            var objectList = new List<object> { baz };
            var scalarList = new List<object> { 123 };

            var result = new QueryResult();

            result.Data["foo"] = objectList;
            result.Data["bar"] = scalarList;
            result.Data["baz"] = baz;

            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("foo")
                .SetPath(Path.New("root").Append("child"))
                .AddLocation(new Location(15, 16))
                .SetExtension("bar", "baz")
                .Build());

            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("qux")
                .SetExtension("bar", "baz")
                .Build());

            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("quux")
                .Build());

            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result, stream);
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

            var result = new QueryResult();

            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("foo")
                .SetPath(Path.New("root").Append("child"))
                .AddLocation(new Location(15, 16))
                .SetExtension("bar", "baz")
                .Build());

            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("qux")
                .SetExtension("bar", "baz")
                .Build());

            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("quux")
                .Build());

            var stream = new MemoryStream();
            var serializer = new JsonQueryResultSerializer();
            await serializer.SerializeAsync(result, stream);
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
