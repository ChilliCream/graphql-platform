namespace StrawberryShake;

public class EntityStoreTests
{
    [Fact]
    public async Task UpdateEntity()
    {
        // arrange
        var entityStore = new EntityStore();
        var entityId = new EntityId(nameof(MockEntity), 1);

        IReadOnlyList<MockEntity> updated = Array.Empty<MockEntity>();
        ulong version = 0;

        using var subscription = entityStore.Watch().Subscribe(update =>
        {
            updated = update.Snapshot.GetEntities<MockEntity>(update.UpdatedEntityIds);
            version = update.Version;
        });

        // act
        entityStore.Update(session =>
        {
            session.SetEntity(entityId, new MockEntity("abc", 1));
        });

        await Task.Delay(250);

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
    public async Task RemoveEntity()
    {
        // arrange
        var cts = new CancellationTokenSource(2000);
        var entityStore = new EntityStore();
        var entityId = new EntityId(nameof(MockEntity), 1);

        entityStore.Update(session =>
        {
            session.SetEntity(entityId, new MockEntity("abc", 1));
        });

        // act
        entityStore.Update(session =>
        {
            session.RemoveEntity(entityId);
        });

        while (entityStore.CurrentSnapshot.GetEntityIds().Count > 0 &&
            !cts.IsCancellationRequested)
        {
            await Task.Delay(50, cts.Token);
        }

        // assert
        Assert.Empty(entityStore.CurrentSnapshot.GetEntityIds());
    }

    [Fact]
    public void GetAllEntities()
    {
        // arrange
        var entityStore = new EntityStore();
        var entityId1 = new EntityId(nameof(MockEntity), 1);
        var entityId2 = new EntityId(nameof(MockEntity) + "a", 2);

        entityStore.Update(session =>
        {
            session.SetEntity(entityId1, new MockEntity("abc", 1));
            session.SetEntity(entityId2, new MockEntity("abc", 2));
        });

        // act
        var allEntities = entityStore.CurrentSnapshot.GetEntities().ToList();
        var mockEntities = entityStore.CurrentSnapshot.GetEntities(nameof(MockEntity));

        // assert
        Assert.Collection(
            allEntities.OrderBy(t => t.Id.Value),
            item =>
            {
                var entity = Assert.IsType<MockEntity>(item.Entity);
                Assert.Equal("abc", entity.Foo);
                Assert.Equal(1, entity.Bar);
            },
            item =>
            {
                var entity = Assert.IsType<MockEntity>(item.Entity);
                Assert.Equal("abc", entity.Foo);
                Assert.Equal(2, entity.Bar);
            });

        Assert.Collection(
            mockEntities.OrderBy(t => t.Id.Value),
            item =>
            {
                var entity = Assert.IsType<MockEntity>(item.Entity);
                Assert.Equal("abc", entity.Foo);
                Assert.Equal(1, entity.Bar);
            });
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
