using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using StrawberryShake.Internal;
using StrawberryShake.Persistence.SQLite;
using Xunit;

namespace StrawberryShake.Persistence.SQLLite
{
    public class LiteDBPersistenceTests
    {
        [Fact]
        public async Task SaveEntities()
        {
            var fileName = Path.GetTempFileName();
            File.Delete(fileName);

            try
            {
                using var ct = new CancellationTokenSource(20_000);
                var entityStore = new EntityStore();
                using var operationStore = new OperationStore(entityStore);
                var storeAccessor = new MockStoreAccessor(operationStore, entityStore);
                using var db = new LiteDatabase(fileName);
                using var persistence = new LiteDBPersistence(storeAccessor, db);

                persistence.BeginInitialize();

                await Task.Delay(250, ct.Token);

                entityStore.Update(session =>
                {
                    session.SetEntity(new EntityId("ABC", 1), new MockEntity("abc"));
                });

                var count = 0;

                while (!ct.IsCancellationRequested && count == 0)
                {
                    await Task.Delay(50, ct.Token);
                    count = db.GetCollection(LiteDBPersistence.Entities).Count();
                }

                Assert.Equal(1, count);
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [Fact]
        public async Task LoadEntities()
        {
            var fileName = Path.GetTempFileName();
            File.Delete(fileName);

            try
            {
                {
                    using var ct = new CancellationTokenSource(20_000);
                    var entityStore = new EntityStore();
                    using var operationStore = new OperationStore(entityStore);
                    using var db = new LiteDatabase(fileName);
                    var storeAccessor = new MockStoreAccessor(operationStore, entityStore);
                    using var persistence = new LiteDBPersistence(storeAccessor, db);

                    persistence.BeginInitialize();

                    await Task.Delay(250, ct.Token);

                    entityStore.Update(session =>
                    {
                        session.SetEntity(new EntityId("ABC", 1), new MockEntity("abc"));
                    });

                    var count = 0;

                    while (!ct.IsCancellationRequested && count == 0)
                    {
                        await Task.Delay(50, ct.Token);
                        count = db.GetCollection("entities").Count();
                    }

                    Assert.Equal(1, count);
                }

                // now we recreate the context and should get entities from the database
                {
                    using var ct = new CancellationTokenSource(20_000);
                    var entityStore = new EntityStore();
                    using var operationStore = new OperationStore(entityStore);
                    var storeAccessor = new MockStoreAccessor(operationStore, entityStore);
                    using var db = new LiteDatabase(fileName);
                    using var persistence = new LiteDBPersistence(storeAccessor, db);

                    persistence.BeginInitialize();

                    await Task.Delay(500, ct.Token);

                    Assert.Collection(
                        storeAccessor.EntityStore.CurrentSnapshot.GetEntities(),
                        info =>
                        {
                            Assert.Equal("ABC", info.Id.Name);
                            Assert.Equal(1, info.Id.Value);
                            MockEntity entity = Assert.IsType<MockEntity>(info.Entity);
                            Assert.Equal("abc", entity.Name);
                        });
                }
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        public class MockEntity
        {
            public MockEntity(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public class MockStoreAccessor : StoreAccessor
        {
            public MockStoreAccessor(
                IOperationStore operationStore,
                IEntityStore entityStore)
                : base(
                    operationStore,
                    entityStore,
                    new MockEntityIdSerializer(),
                    new IOperationRequestFactory[0],
                    new IOperationResultDataFactory[0])
            {
            }
        }

        public class MockEntityIdSerializer : IEntityIdSerializer
        {
            public EntityId Parse(JsonElement obj)
            {
                return new(
                    obj.GetProperty("__typename").GetString()!,
                    obj.GetProperty("id").GetInt32());
            }

            public string Format(EntityId entityId)
            {
                using var writer = new ArrayWriter();
                using var jsonWriter = new Utf8JsonWriter(writer);
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("__typename", entityId.Name);
                jsonWriter.WriteNumber("id", (int)entityId.Value);
                jsonWriter.WriteEndObject();
                jsonWriter.Flush();
                return Encoding.UTF8.GetString(writer.GetInternalBuffer(), 0, writer.Length);
            }
        }
    }
}
