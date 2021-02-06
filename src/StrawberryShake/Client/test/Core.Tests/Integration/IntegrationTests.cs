using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using StrawberryShake.Integration.Mappers;
using StrawberryShake.Serialization;
using Xunit;

namespace StrawberryShake.Integration
{
    public class IntegrationTests
    {
        [Fact(Skip = "MST FIX")]
        public async Task Foo()
        {
            var entityStore = new EntityStore();
            var operationStore = new OperationStore(entityStore.Watch());

            var connection = new MockConnection();

            var humanMapper = new HumanMapper();
            var droidMapper = new DroidMapper();

            var humanHeroMapper = new HumanHeroMapper(
                entityStore,
                humanMapper,
                droidMapper);

            var droidHeroMapper = new DroidHeroMapper(
                entityStore,
                humanMapper,
                droidMapper);

            var resultDataFactory = new GetHeroResultFactory(
                entityStore,
                humanHeroMapper,
                droidHeroMapper);

            var resultBuilder = new GetHeroResultBuilder(
                entityStore,
                ExtractEntityId,
                resultDataFactory,
                new SerializerResolver(new[] { new StringSerializer() }));

            var operationExecutor = new OperationExecutor<JsonDocument, GetHeroResult>(
                connection,
                () => resultBuilder,
                operationStore,
                ExecutionStrategy.NetworkOnly);

            GetHeroResult? result = null;
            var query = new GetHeroQuery(operationExecutor);
            query.Watch().Subscribe(c =>
            {
                result = c.Data;
            });

            await Task.Delay(1000);

            using (entityStore.BeginUpdate())
            {
                DroidEntity entity = entityStore.GetOrCreate<DroidEntity>(new EntityId("Droid", "abc"));
                entity.Name = "C3-PO";
            }

            await Task.Delay(1000);

            Assert.NotNull(result);

            EntityId ExtractEntityId(JsonElement obj) =>
                new(
                    obj.GetProperty("__typename").GetString()!,
                    obj.GetProperty("id").GetString()!);
        }

        public class MockConnection : IConnection<JsonDocument>
        {
            public async IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(
                OperationRequest request,
                CancellationToken cancellationToken = default)
            {
                string json = FileResource.Open("GetHeroResult.json");
                yield return new Response<JsonDocument>(JsonDocument.Parse(json), null);
            }
        }
    }
}
