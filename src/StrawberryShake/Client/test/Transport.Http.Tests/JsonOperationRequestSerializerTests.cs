using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Transport.Http
{
    public class JsonOperationRequestSerializerTests
    {
        [Fact]
        public void Serialize_Request_With_InputObject()
        {
            // arrange
            var inputObject = new KeyValuePair<string, object?>[]
            {
                new("s", "def"),
                new("i", 123),
                new("d", 123.123),
                new("b", true),
                new("ol", new List<object>
                {
                    new KeyValuePair<string, object?>[]
                    {
                        new("s", "def"),
                    }
                }),
                new("sl", new List<string> { "a", "b", "c" }),
                new("il", new[] { 1, 2, 3 })
            };

            // act
            using var stream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(stream, new() { Indented = true });
            var serializer = new JsonOperationRequestSerializer();
            serializer.Serialize(
                new OperationRequest(
                    "abc",
                    new Document(),
                    new Dictionary<string, object?> { { "abc", inputObject } }),
                jsonWriter);
            jsonWriter.Flush();

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
        }

        private class Document : IDocument
        {
            public OperationKind Kind => OperationKind.Query;
            public ReadOnlySpan<byte> Body => Encoding.UTF8.GetBytes("{ __typename }");
        }
    }
}
