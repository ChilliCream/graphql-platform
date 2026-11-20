using System.Text.Json;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class Issue5228Tests(MongoResource resource) : ProjectionVisitorTestBase, IClassFixture<MongoResource>
{
    [Fact]
    public async Task Nested_Collection_Filter_Should_Filter_Child_Items()
    {
        var matchingId = Guid.Parse("f00554bc-fcef-417c-baee-9234534cb6bd");
        var nonMatchingId = Guid.Parse("f00554bc-fcef-417c-baee-9234534cb6be");

        var executor = CreateSchema(
            entities:
            [
                new GatewayDefinition
                {
                    Name = "Test",
                    ModelReferences =
                    [
                        new ModelReference
                        {
                            Id = matchingId,
                            DomainModelId = Guid.NewGuid(),
                            Depth = 2
                        }
                    ]
                }
            ],
            mongoResource: resource,
            useOffsetPaging: true);

        var result = await executor.ExecuteAsync(
            $$"""
            {
              root {
                items {
                  name
                  modelReferences(where: { id: { eq: "{{nonMatchingId}}" } }) {
                    id
                  }
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        using var json = JsonDocument.Parse(result.ToJson());
        var modelReferences = json.RootElement
            .GetProperty("data")
            .GetProperty("root")
            .GetProperty("items")[0]
            .GetProperty("modelReferences");

        Assert.Equal(0, modelReferences.GetArrayLength());
    }

    public class GatewayDefinition
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        [UseFiltering]
        [UseSorting]
        public List<ModelReference> ModelReferences { get; set; } = [];
    }

    public class ModelReference
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid DomainModelId { get; set; }

        public int Depth { get; set; }
    }
}
