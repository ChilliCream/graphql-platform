using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

public class OperationResultItemCollectionTests
{
    [Fact]
    public void Dispose_Should_ReleaseOnce_When_DisposedRepeatedly()
    {
        // arrange
        var items = new CountingItemCollection();

        // act
        items.Dispose();
        items.Dispose();
        items.Dispose();

        // assert
        Assert.Equal(1, items.ReleaseCount);
    }

    [Fact]
    public void Dispose_Should_ReleaseOnce_When_DisposedConcurrently()
    {
        // arrange
        var items = new CountingItemCollection();

        // act
        Parallel.For(0, 64, _ => items.Dispose());

        // assert
        Assert.Equal(1, items.ReleaseCount);
    }

    private sealed class CountingItemCollection : OperationResultItemCollection
    {
        private int _releaseCount;

        public int ReleaseCount => _releaseCount;

        public override void WriteDataTo(JsonWriter jsonWriter)
            => jsonWriter.WriteNullValue();

        public override IEnumerator<OperationResultItem> GetEnumerator()
            => Enumerable.Empty<OperationResultItem>().GetEnumerator();

        protected override void Release()
            => Interlocked.Increment(ref _releaseCount);
    }
}
