using System.Buffers;
using System.Text.Json;

namespace StrawberryShake;

public class OperationResultBuilderTests
{
    [Fact]
    public void Build_With_Extensions()
    {
        // arrange
        var factory = new DocumentDataFactory();
        var builder = new DocumentOperationResultBuilder(factory);

        // According to the current design, all implementations of IConnection operate
        // on JsonDocuments which are deserialized straight from response streams and
        // very few properties in the Response object are in fact ever initialized,
        // including Response.Extensions. It is therefore safe to assume that, at
        // least for now, OperationResultBuilder is the best place to actually parse
        // and extract "extensions".
        var body = JsonDocument.Parse(@"{""data"": { }, ""extensions"": { ""a"": 1, ""b"": { ""c"": ""Strawberry"" }, ""d"": 3.14 } }");
        var response = new Response<JsonDocument>(body, null);

        // act
        var result = builder.Build(response);

        // assert
        Assert.NotEmpty(result.Extensions);
        Assert.Equal(1L, result.Extensions["a"]);

        var b = (IReadOnlyDictionary<string, object?>?)result.Extensions["b"];

        Assert.NotNull(b);
        Assert.Equal("Strawberry", b["c"]);

        Assert.Equal(3.14, result.Extensions["d"]);
    }

    [Fact]
    public void Build_With_Capture_Stores_Data_Payload_In_ContextData()
    {
        // arrange
        var builder = new RecordingOperationResultBuilder(new DocumentDataFactory());
        var response = new Response<JsonDocument>(
            JsonDocument.Parse(@"{""data"": { ""name"": ""Strawberry"" } }"),
            null);

        // act
        var result = builder.Build(response);

        // assert
        Assert.True(
            result.ContextData.TryGetValue(WellKnownContextData.PersistedData, out var payload));
        var element = Assert.IsType<JsonElement>(payload);
        Assert.Equal("Strawberry", element.GetProperty("name").GetString());
    }

    [Fact]
    public void Build_Without_Capture_Does_Not_Store_Data_Payload()
    {
        // arrange
        var builder = new DocumentOperationResultBuilder(new DocumentDataFactory());
        var response = new Response<JsonDocument>(
            JsonDocument.Parse(@"{""data"": { ""name"": ""Strawberry"" } }"),
            null);

        // act
        var result = builder.Build(response);

        // assert
        Assert.Empty(result.ContextData);
    }

    [Fact]
    public void BuildFromPersistedData_RoundTrips_The_Captured_Payload()
    {
        // arrange
        var builder = new RecordingOperationResultBuilder(new DocumentDataFactory());
        var captured = builder.Build(
            new Response<JsonDocument>(
                JsonDocument.Parse(@"{""data"": { ""name"": ""Strawberry"" } }"),
                null));
        var payload = (JsonElement)captured.ContextData[WellKnownContextData.PersistedData]!;

        // act
        var rehydrated = builder.BuildFromPersistedData(ToUtf8(payload));

        // assert
        Assert.NotNull(rehydrated.Data);
        Assert.Equal("Strawberry", builder.LastData!.Value.GetProperty("name").GetString());
    }

    private static ReadOnlyMemory<byte> ToUtf8(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            element.WriteTo(writer);
        }

        return buffer.WrittenMemory;
    }

    internal class Document;

    internal class DocumentDataInfo : IOperationResultDataInfo
    {
        public IReadOnlyCollection<EntityId> EntityIds { get; } = ArraySegment<EntityId>.Empty;

        public ulong Version { get; }

        public IOperationResultDataInfo WithVersion(ulong version)
        {
            throw new NotImplementedException();
        }
    }

    internal class DocumentDataFactory : IOperationResultDataFactory<Document>
    {
        public Type ResultType { get => typeof(Document); }

        public Document Create(IOperationResultDataInfo dataInfo, IEntityStoreSnapshot? snapshot = null)
        {
            return new Document();
        }

        object IOperationResultDataFactory.Create(IOperationResultDataInfo dataInfo, IEntityStoreSnapshot? snapshot)
        {
            return Create(dataInfo, snapshot);
        }
    }

    internal class DocumentOperationResultBuilder : OperationResultBuilder<Document>
    {
        public DocumentOperationResultBuilder(IOperationResultDataFactory<Document> resultDataFactory)
        {
            ResultDataFactory = resultDataFactory;
        }

        protected override IOperationResultDataFactory<Document> ResultDataFactory { get; }

        protected override IOperationResultDataInfo BuildData(JsonElement obj)
        {
            return new DocumentDataInfo();
        }
    }

    internal sealed class RecordingOperationResultBuilder : OperationResultBuilder<Document>
    {
        public RecordingOperationResultBuilder(
            IOperationResultDataFactory<Document> resultDataFactory)
        {
            ResultDataFactory = resultDataFactory;
        }

        public JsonElement? LastData { get; private set; }

        protected override bool CapturePersistedData => true;

        protected override IOperationResultDataFactory<Document> ResultDataFactory { get; }

        protected override IOperationResultDataInfo BuildData(JsonElement obj)
        {
            LastData = obj.Clone();
            return new DocumentDataInfo();
        }
    }
}
