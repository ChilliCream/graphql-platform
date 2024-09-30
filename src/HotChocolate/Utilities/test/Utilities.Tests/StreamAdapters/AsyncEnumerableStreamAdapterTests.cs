using Xunit;

namespace HotChocolate.Utilities.StreamAdapters;

public class AsyncEnumerableStreamAdapterTests
{
    private readonly string[] _strings = ["a", "b", "c", "d", "e",];

    [Fact]
    public async Task ArrayToStream()
    {
        // arrange
        var list = new List<object?>();
        var asyncEnumerable = new TestEnumerable(_strings);
        var adapter = new AsyncEnumerableStreamAdapter<string>(asyncEnumerable);

        // act
        await foreach (var item in adapter)
        {
            list.Add(item);
        }

        // assert
        for (var i = 0; i < list.Count; i++)
        {
            Assert.Equal(_strings[i], list[i]);
        }
    }

    [Fact]
    public async Task ArrayToStream_Cancel()
    {
        // arrange
        var list = new List<object?>();
        var asyncEnumerable = new TestEnumerable(_strings);
        var adapter = new AsyncEnumerableStreamAdapter<string>(asyncEnumerable);
        var cts = new CancellationTokenSource();

        // act
        await foreach (var item in adapter.WithCancellation(cts.Token))
        {
            list.Add(item);
            cts.Cancel();
        }

        // assert
        Assert.Collection(list, s => Assert.Equal(_strings[0], s));
    }

    [Fact]
    public void ArrayToStream_QueryIsNull()
    {
        // arrange
        // act
        void Verify() => new AsyncEnumerableStreamAdapter<string>(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }

    private sealed class TestEnumerable : IAsyncEnumerable<string>
    {
        private readonly IEnumerable<string> _strings;

        public TestEnumerable(IEnumerable<string> strings)
        {
            _strings = strings;
        }

        public IAsyncEnumerator<string> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
            => new Enumerator(_strings.GetEnumerator());

        private sealed class Enumerator : IAsyncEnumerator<string>
        {
            private readonly IEnumerator<string> _enumerator;

            public Enumerator(IEnumerator<string> enumerator)
            {
                _enumerator = enumerator;
            }

            public string Current => _enumerator.Current;

            public ValueTask<bool> MoveNextAsync()
                => new(_enumerator.MoveNext());

            public ValueTask DisposeAsync()
            {
                _enumerator.Dispose();
                return default;
            }
        }
    }
}
