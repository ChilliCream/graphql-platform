using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using StrawberryShake.Serialization;
using StrawberryShake.Transport.Http;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Foo()
        {
            var entityStore = new EntityStore();
            var operationStore = new OperationStore(entityStore.Watch());

            var connection = new MockConnection();

            var humanMapper = new GetHero_Hero_DroidFromDroidEntityMapper();
            var droidMapper = new GetHero_Hero_HumanFromHumanEntityMapper();

            var resultDataFactory = new GetHeroFactory(
                entityStore,
                humanMapper,
                droidMapper);

            var resultBuilder = new GetHeroBuilder(
                entityStore,
                EntityIdFactory.CreateEntityId,
                resultDataFactory,
                new SerializerResolver(new ISerializer[]
                {
                    new StringSerializer(),
                    new EpisodeParser()
                }));

            var operationExecutor = new OperationExecutor<JsonDocument, IGetHero>(
                connection,
                () => resultBuilder,
                operationStore,
                ExecutionStrategy.NetworkOnly);

            IGetHero? result = null;
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
