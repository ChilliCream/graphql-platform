using Xunit;

namespace HotChocolate.Utilities.StreamAdapters;

public class QueryableStreamAdapterTests
{
    private readonly string[] _strings = ["a", "b", "c", "d", "e",];
    private readonly object[] _objects = ["a", "b", "c", "d", "e",];

    [Fact]
    public async Task QueryableToStream()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new QueryableStreamAdapter<string>(_strings.AsQueryable());

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
    public async Task Objects_QueryableToStream()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new QueryableStreamAdapter(_objects.AsQueryable());

        // act
        await foreach (var item in adapter)
        {
            list.Add(item);
        }

        // assert
        for (var i = 0; i < list.Count; i++)
        {
            Assert.Equal(_objects[i], list[i]);
        }
    }

    [Fact]
    public async Task QueryableToStream_Cancel()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new QueryableStreamAdapter<string>(_strings.AsQueryable());
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
    public async Task Objects_QueryableToStream_Cancel()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new QueryableStreamAdapter(_objects.AsQueryable());
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
    public void QueryableToStream_QueryIsNull()
    {
        // arrange
        // act
        void Verify() => new QueryableStreamAdapter<string>(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void Objects_QueryableToStream_QueryIsNull()
    {
        // arrange
        // act
        void Verify() => new QueryableStreamAdapter(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }
}
