using System.Collections;
using System.Linq.Expressions;

namespace HotChocolate;

public static class AsyncEnumerableExecutableTests
{
    [Fact]
    public static async Task EmptyAsyncEnumerable_FirstOrDefault_Null()
    {
        // arrange
        IAsyncEnumerable<string> query = new EmptyAsyncEnumerable();

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public static async Task ListAsyncEnumerable_FirstOrDefault_A()
    {
        // arrange
        IAsyncEnumerable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Equal("a", result);
    }

    [Fact]
    public static async Task EmptyAsyncEnumerable_SingleOrDefault_Null()
    {
        // arrange
        IAsyncEnumerable<string> query = new EmptyAsyncEnumerable();

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public static async Task ListAsyncEnumerable_SingleOrDefault_A()
    {
        // arrange
        IAsyncEnumerable<string> query = new ListAsyncEnumerable(["a"]);

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Equal("a", result);
    }

    [Fact]
    public static async Task ListAsyncEnumerable_SingleOrDefault_Throw()
    {
        // arrange
        IAsyncEnumerable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        async Task Error() => await Executable.From(query).SingleOrDefaultAsync();

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(Error);
    }

    [Fact]
    public static async Task ListAsyncEnumerable_ToListAsync()
    {
        // arrange
        IAsyncEnumerable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        var result = await Executable.From(query).ToListAsync();

        // assert
        Assert.Collection(
            result,
            r => Assert.Equal("a", r),
            r => Assert.Equal("b", r));
    }

    [Fact]
    public static async Task ListAsyncEnumerable_ToAsyncEnumerable()
    {
        // arrange
        IAsyncEnumerable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        var result = Executable.From(query).ToAsyncEnumerable();

        // assert
        await foreach (var item in result)
        {
            Assert.True(item is "a" or "b");
        }
    }

    private class EmptyAsyncEnumerable : IQueryable<string>, IAsyncEnumerable<string>
    {
        public Type ElementType
            => throw new NotSupportedException();

        public Expression Expression
            => throw new NotSupportedException();

        public IQueryProvider Provider
            => throw new NotSupportedException();

        public IEnumerator<string> GetEnumerator()
            => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator()
            => throw new NotSupportedException();

        public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await new ValueTask();
            yield break;
        }
    }

    private class ListAsyncEnumerable(string[] items) : IQueryable<string>, IAsyncEnumerable<string>
    {
        public Type ElementType
            => throw new NotSupportedException();

        public Expression Expression
            => throw new NotSupportedException();

        public IQueryProvider Provider
            => throw new NotSupportedException();

        public IEnumerator<string> GetEnumerator()
            => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator()
            => throw new NotSupportedException();

        public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await new ValueTask();

            foreach (var item in items)
            {
                yield return item;
            }
        }
    }
}
