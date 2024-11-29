using Xunit;

namespace HotChocolate.Utilities.StreamAdapters;

public class EnumerableStreamAdapterTests
{
    private readonly string[] _strings = ["a", "b", "c", "d", "e",];
    private readonly object[] _objects = ["a", "b", "c", "d", "e",];

    [Fact]
    public async Task ArrayToStream()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new EnumerableStreamAdapter<string>(_strings);

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
    public async Task Objects_ArrayToStream()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new EnumerableStreamAdapter(_objects.AsQueryable());

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
    public async Task ArrayToStream_Cancel()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new EnumerableStreamAdapter<string>(_strings.AsQueryable());
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
    public async Task Objects_ArrayToStream_Cancel()
    {
        // arrange
        var list = new List<object?>();
        var adapter = new EnumerableStreamAdapter(_objects.AsQueryable());
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
        void Verify() => new EnumerableStreamAdapter<string>(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void Objects_ArrayToStream_QueryIsNull()
    {
        // arrange
        // act
        void Verify() => new EnumerableStreamAdapter(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }
}
