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
        Assert.Equal("Strawberry", b?["c"]);

        Assert.Equal(3.14, result.Extensions["d"]);
    }

    internal class Document
    {
    }

    internal class DocumentDataInfo : IOperationResultDataInfo
    {
        public IReadOnlyCollection<EntityId> EntityIds { get; } = ArraySegment<EntityId>.Empty;

        public ulong Version { get; } = 0;

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
}
