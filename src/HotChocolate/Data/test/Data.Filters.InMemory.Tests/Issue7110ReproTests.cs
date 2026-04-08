using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters;

public class Issue7110ReproTests : IClassFixture<SchemaCache>
{
    private readonly SchemaCache _cache;

    public Issue7110ReproTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task GuidRelayId_Filter_Equals_Does_Not_Throw_ConversionError()
    {
        EntityWithGuidId[] values =
            [
                new EntityWithGuidId { Id = Guid.Parse("84e9a610-aeb4-4e20-aeda-cf85d0af8d61") },
                new EntityWithGuidId { Id = Guid.Parse("94e9a610-aeb4-4e20-aeda-cf85d0af8d62") }
            ];

        var executor = _cache.CreateSchema<EntityWithGuidId, EntityWithGuidIdFilterInput>(
            values,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        var idsResult = await executor.ExecuteAsync("{ root { id } }");
        using var idsDocument = JsonDocument.Parse(idsResult.ToJson());
        var relayId = idsDocument
            .RootElement
            .GetProperty("data")
            .GetProperty("root")[0]
            .GetProperty("id")
            .GetString();

        Assert.NotNull(relayId);

        var filteredResult =
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($id: ID!) {
                          root(where: { id: { eq: $id } }) {
                            id
                          }
                        }
                        """)
                    .SetVariableValues(new Dictionary<string, object?> { ["id"] = relayId! })
                    .Build());

        var operationResult = filteredResult.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    public class EntityWithGuidId
    {
        [ID]
        public Guid Id { get; set; }
    }

    public class EntityWithGuidIdFilterInput : FilterInputType<EntityWithGuidId>
    {
        protected override void Configure(IFilterInputTypeDescriptor<EntityWithGuidId> descriptor)
        {
            descriptor.Field(t => t.Id);
        }
    }
}
