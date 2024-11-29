using System.Text;
using System.Text.Json;

namespace StrawberryShake.Json;

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
                    },
                }),
                new("sl", new List<string> { "a", "b", "c", }),
                new("il", new[] { 1, 2, 3, }),
        };

        // act
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream, new() { Indented = true, });
        var serializer = new JsonOperationRequestSerializer();
        serializer.Serialize(
            new OperationRequest(
                "abc",
                new Document(),
                new Dictionary<string, object?> { { "abc", inputObject }, }),
            jsonWriter);
        jsonWriter.Flush();

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
    }

    [Fact]
    public void Serialize_Request_With_Json()
    {
        // arrange
        var json = JsonDocument.Parse(@"{ ""abc"": { ""def"": ""def"" } }");

        // act
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream, new() { Indented = true, });
        var serializer = new JsonOperationRequestSerializer();
        serializer.Serialize(
            new OperationRequest(
                "abc",
                new Document(),
                new Dictionary<string, object?> { { "abc", json.RootElement }, }),
            jsonWriter);
        jsonWriter.Flush();

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
    }

    [Fact]
    public void Serialize_Request_With_Id_And_Empty_Query()
    {
        // arrange
        var json = JsonDocument.Parse(@"{ ""abc"": { ""def"": ""def"" } }");

        // act
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream, new() { Indented = true, });
        var serializer = new JsonOperationRequestSerializer();
        serializer.Serialize(
            new OperationRequest(
                "123",
                "abc",
                new EmptyDocument(),
                new Dictionary<string, object?> { { "abc", json.RootElement }, },
                strategy: RequestStrategy.PersistedOperation),
            jsonWriter);
        jsonWriter.Flush();

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
    }

    [Fact]
    public void Serialize_Request_With_Extensions()
    {
        // arrange
        var operationRequest = new OperationRequest(
            "abc",
            new Document());
        operationRequest.Extensions.Add(nameof(String), "def");
        operationRequest.Extensions.Add("null", null);

        operationRequest.Extensions.Add(nameof(Byte), (byte)123);
        operationRequest.Extensions.Add(nameof(Int16), (short)123);
        operationRequest.Extensions.Add(nameof(UInt16), (ushort)123);
        operationRequest.Extensions.Add(nameof(Int32), 123);
        operationRequest.Extensions.Add(nameof(UInt32), (uint)123);
        operationRequest.Extensions.Add(nameof(Int64), (long)123);
        operationRequest.Extensions.Add(nameof(UInt64), (ulong)123);

        operationRequest.Extensions.Add(nameof(Single), (float)123.123);
        operationRequest.Extensions.Add(nameof(Double), 123.123);
        operationRequest.Extensions.Add(nameof(Decimal), (decimal)123.123);

        operationRequest.Extensions.Add(nameof(Uri), new Uri("http://local"));

        operationRequest.Extensions.Add("ol",
            new List<object>
            {
                    new KeyValuePair<string, object?>[]
                    {
                        new("s", "def"),
                    },
            });
        operationRequest.Extensions.Add("sl", new List<string> { "a", "b", "c", });
        operationRequest.Extensions.Add("il", new[] { 1, 2, 3, });
        operationRequest.Extensions.Add("tuple", ("a", "b"));

        // act
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream, new() { Indented = true, });
        var serializer = new JsonOperationRequestSerializer();
        serializer.Serialize(operationRequest, jsonWriter);
        jsonWriter.Flush();

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
    }

    private sealed class Document : IDocument
    {
        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => Encoding.UTF8.GetBytes("{ __typename }");

        public DocumentHash Hash { get; } = new("MD5", "ABCDEF");
    }

    private sealed class EmptyDocument : IDocument
    {
        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => [];

        public DocumentHash Hash { get; } = new("MD5", "ABCDEF");
    }
}
