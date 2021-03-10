using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Moq;
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
                var db = new LiteDatabase(fileName);
                var storeAccessor = new MockStoreAccessor(operationStore, entityStore);
                using var persistence = new LiteDBPersistence(storeAccessor, db);

                persistence.Begin();

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
                    var db = new LiteDatabase(fileName);
                    var storeAccessor = new MockStoreAccessor(operationStore, entityStore);
                    using var persistence = new LiteDBPersistence(storeAccessor, db);

                    persistence.Begin();

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

                {
                    using var ct = new CancellationTokenSource(20_000);
                    var entityStore = new EntityStore();
                    using var operationStore = new OperationStore(entityStore);
                    var db = new LiteDatabase(fileName);
                    var storeAccessor = new MockStoreAccessor(operationStore, entityStore);
                    using var persistence = new LiteDBPersistence(storeAccessor, db);

                    persistence.Begin();

                    await Task.Delay(500, ct.Token);

                    Assert.Collection(
                        storeAccessor.EntityStore.CurrentSnapshot.GetEntities(),
                        info =>
                        {
                            Assert.Equal("ABC", info.Id.Name);
                            Assert.Equal(1L, info.Id.Value);
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

        public class MockStoreAccessor : IStoreAccessor
        {
            public MockStoreAccessor(IOperationStore operationStore, IEntityStore entityStore)
            {
                OperationStore = operationStore;
                EntityStore = entityStore;
            }

            public IOperationStore OperationStore { get; }
            public IEntityStore EntityStore { get; }
        }
    }
}
