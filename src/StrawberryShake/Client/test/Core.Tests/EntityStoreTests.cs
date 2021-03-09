using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
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
                updated = update.Snapshot.GetEntities<MockEntity>(update.UpdatedEntityIds);
                version = update.Version;
            });

            // act
            entityStore.Update(session =>
            {
                session.SetEntity(entityId, new MockEntity("abc", 1));
            });

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
        public void RemoveEntity()
        {
            // arrange
            var entityStore = new EntityStore();
            var entityId = new EntityId(nameof(MockEntity), 1);

            ISet<EntityId> updated = new HashSet<EntityId>();
            ulong version = 0;

            entityStore.Update(session =>
            {
                session.SetEntity(entityId, new MockEntity("abc", 1));
            });

            entityStore.Watch().Subscribe(update =>
            {
                updated = update.UpdatedEntityIds;
                version = update.Version;
            });

            // act
            entityStore.Update(session =>
            {
                session.RemoveEntity(entityId);
            });

            // assert
            Assert.Empty(entityStore.CurrentSnapshot.GetEntityIds());
            Assert.Collection(
                updated,
                item =>
                {
                    Assert.Equal(entityId, item);
                });
            Assert.Equal(2ul, version);
        }

        public class MockEntity
        {
            public MockEntity(string? foo, int bar)
            {
                Foo = foo;
                Bar = bar;
            }

            public string? Foo { get; }

            public int Bar { get; }
        }
    }
}
