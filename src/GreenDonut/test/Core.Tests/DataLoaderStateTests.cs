using Xunit;

namespace GreenDonut;

public static class DataLoaderStateTests
{
    [Fact]
    public static async Task SetStateInferredKey()
    {
        // arrange
        var loader = new DummyDataLoader(typeof(string).FullName!);

        // act
        await loader.SetState("abc").LoadAsync("def");

        // assert
        Assert.Equal("abc", loader.State);
    }

    [Fact]
    public static async Task SetStateExplicitKey()
    {
        // arrange
        var loader = new DummyDataLoader("abc");

        // act
        await loader.SetState("abc", "def").LoadAsync("ghi");

        // assert
        Assert.Equal("def", loader.State);
    }

    [Fact]
    public static async Task TrySetStateInferredKey()
    {
        // arrange
        var loader = new DummyDataLoader(typeof(string).FullName!);

        // act
        await loader.SetState("abc").TrySetState("xyz").LoadAsync("def");

        // assert
        Assert.Equal("abc", loader.State);
    }

    [Fact]
    public static async Task TrySetStateExplicitKey()
    {
        // arrange
        var loader = new DummyDataLoader("abc");

        // act
        await loader.SetState("abc", "def").TrySetState("abc", "xyz").LoadAsync("def");

        // assert
        Assert.Equal("def", loader.State);
    }

    [Fact]
    public static async Task AddStateEnumerableInferredKey()
    {
        // arrange
        var loader = new DummyDataLoader(typeof(string).FullName!);

        // act
        await loader.AddStateEnumerable("abc").AddStateEnumerable("xyz").LoadAsync("def");

        // assert
        Assert.Collection(
            (IEnumerable<string>)loader.State!,
            item => Assert.Equal("abc", item),
            item => Assert.Equal("xyz", item));
    }

    [Fact]
    public static async Task AddStateEnumerableExplicitKey()
    {
        // arrange
        var loader = new DummyDataLoader("abc");

        // act
        await loader.AddStateEnumerable("abc", "def").AddStateEnumerable("abc", "xyz").LoadAsync("def");

        // assert
        Assert.Collection(
            (IEnumerable<string>)loader.State!,
            item => Assert.Equal("def", item),
            item => Assert.Equal("xyz", item));
    }

    public class DummyDataLoader(string expectedKey, DataLoaderOptions? options = null)
        : DataLoaderBase<string, string>(AutoBatchScheduler.Default, options)
    {
        public object? State { get; set; }

        protected internal override ValueTask FetchAsync(
            IReadOnlyList<string> keys,
            Memory<Result<string?>> results,
            DataLoaderFetchContext<string> context,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                results.Span[i] = keys[i];
            }

            State = context.GetState<object?>(expectedKey);

            return default;
        }
    }
}
