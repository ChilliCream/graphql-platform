using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace StrawberryShake
{
    public class EntityStoreTests
    {
        [Fact]
        public void UpdateEntity()
        {
            // arrange
            var entityStore = new EntityStore();
            var entityId = new EntityId(nameof(MockEntity), 1);

            IReadOnlyList<MockEntity> updated = Array.Empty<MockEntity>();
            ulong version = 0;

            entityStore.Watch().Subscribe(update =>
            {
                updated = entityStore.GetEntities<MockEntity>(update.UpdatedEntityIds);
                version = update.Version;
            });

            // act
            using (entityStore.BeginUpdate())
            {
                MockEntity entity = entityStore.GetOrCreate<MockEntity>(entityId);
                entity.Foo = "abc";
                entity.Bar = 1;
            }

            // assert
            Assert.Collection(
                updated,
                item =>
                {
                    Assert.Equal("abc", item.Foo);
                    Assert.Equal(1, item.Bar);
                });
            Assert.Equal(1ul, version);
        }

        [Fact]
        public async Task EnsureUpdatesAreExecutedOneAfterTheOther()
        {
            // arrange
            var entityStore = new EntityStore();
            var entityId = new EntityId(nameof(MockEntity), 1);

            List<string> updated = new();
            ulong version = 0;

            entityStore.Watch().Subscribe(update =>
            {
                updated.Add(
                    entityStore.GetEntities<MockEntity>(update.UpdatedEntityIds).Single().Foo!);
                version = update.Version;
            });

            // act
            Task task1 = BeginUpdate(entityStore, "abc");
            Task task2 = BeginUpdate(entityStore, "def");
            await Task.WhenAll(task1, task2);

            // assert
            Assert.Collection(
                updated,
                item =>
                {
                    Assert.Equal("abc", item);
                },
                item =>
                {
                    Assert.Equal("def", item);
                });
            Assert.Equal(2ul, version);

            Task BeginUpdate(IEntityStore entityStore, string foo)
            {
                IEntityUpdateSession session = entityStore.BeginUpdate();

                return Task.Run(async () =>
                {
                    await Task.Delay(50);
                    MockEntity entity = entityStore.GetOrCreate<MockEntity>(entityId);
                    entity.Foo = foo;
                    session.Dispose();
                });
            }
        }

        public class MockEntity
        {
            public string? Foo { get; set; }

            public int Bar { get; set; }
        }
    }
}
