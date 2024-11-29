using System.Collections;
using System.Linq.Expressions;

namespace HotChocolate;

public static class QueryableExecutableTests
{
    [Fact]
    public static void Queryable_Is_Null_Throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Executable.From(((IQueryable<string>?)null)!));
    }

    [Fact]
    public static void Queryable_Print()
    {
        // arrange
        var query = Array.Empty<string>().AsQueryable();

        // act
        var executable = Executable.From(query, _ => "foo");

        // assert
        Assert.Equal("foo", executable.Print());
    }

    [Fact]
    public static void Queryable_Print_Snapshot()
    {
        // arrange
        var query = Array.Empty<string>().AsQueryable();

        // act
        var executable = Executable.From(query);

        // assert
        executable.Print()
            .MatchInlineSnapshot(
                """
                System.String[]
                """);
    }

    [Fact]
    public static void Queryable_Source_Is_Queryable()
    {
        // arrange
        var query = Array.Empty<string>().AsQueryable();
        var executable = Executable.From(query);

        // act
        var source = executable.Source;

        // assert
        Assert.Equal(query, source);
    }

    [Fact]
    public static void Queryable_Source_T_Is_Queryable()
    {
        // arrange
        var query = Array.Empty<string>().AsQueryable();
        var executable = Executable.From(query);

        // act
        // ReSharper disable once RedundantCast
        var source = ((IQueryableExecutable<string>)executable).Source;

        // assert
        Assert.Equal(query, source);
    }

    [Fact]
    public static void Queryable_IsInMemory_True()
    {
        // arrange
        var query = Array.Empty<string>().AsQueryable();
        var executable = Executable.From(query);

        // act
        var isInMemory = executable.IsInMemory;

        // assert
        Assert.True(isInMemory);
    }

    [Fact]
    public static async Task EmptyList_FirstOrDefault_Null()
    {
        // arrange
        var query = Array.Empty<string>().AsQueryable();

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public static async Task List_FirstOrDefault_A()
    {
        // arrange
        var query = new[] { "a", "b" };

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Equal("a", result);
    }

    [Fact]
    public static async Task EmptyAsyncEnumerable_FirstOrDefault_Null()
    {
        // arrange
        IQueryable<string> query = new EmptyAsyncEnumerable();

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public static async Task ListAsyncEnumerable_FirstOrDefault_A()
    {
        // arrange
        IQueryable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Equal("a", result);
    }

    [Fact]
    public static async Task EmptyList_SingleOrDefault_Null()
    {
        // arrange
        var query = Array.Empty<string>().AsQueryable();

        // act
        var result = await Executable.From(query).SingleOrDefaultAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public static async Task List_SingleOrDefault_A()
    {
        // arrange
        var query = new[] { "a" };

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Equal("a", result);
    }

    [Fact]
    public static async Task List_SingleOrDefault_Throw()
    {
        // arrange
        var query = new[] { "a", "b" };

        // act
        async Task Error() => await Executable.From(query).SingleOrDefaultAsync();

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(Error);
    }

    [Fact]
    public static async Task EmptyAsyncEnumerable_SingleOrDefault_Null()
    {
        // arrange
        IQueryable<string> query = new EmptyAsyncEnumerable();

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public static async Task ListAsyncEnumerable_SingleOrDefault_A()
    {
        // arrange
        IQueryable<string> query = new ListAsyncEnumerable(["a"]);

        // act
        var result = await Executable.From(query).FirstOrDefaultAsync();

        // assert
        Assert.Equal("a", result);
    }

    [Fact]
    public static async Task ListAsyncEnumerable_SingleOrDefault_Throw()
    {
        // arrange
        IQueryable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        async Task Error() => await Executable.From(query).SingleOrDefaultAsync();

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(Error);
    }

    [Fact]
    public static async Task List_ToListAsync()
    {
        // arrange
        var query = new[] { "a", "b" }.AsQueryable();

        // act
        var result = await Executable.From(query).ToListAsync();

        // assert
        Assert.Collection(
            result,
            r => Assert.Equal("a", r),
            r => Assert.Equal("b", r));
    }

    [Fact]
    public static async Task ListAsyncEnumerable_ToListAsync()
    {
        // arrange
        IQueryable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        var result = await Executable.From(query).ToListAsync();

        // assert
        Assert.Collection(
            result,
            r => Assert.Equal("a", r),
            r => Assert.Equal("b", r));
    }

    [Fact]
    public static async Task List_ToAsyncEnumerable()
    {
        // arrange
        var query = new[] { "a", "b" }.AsQueryable();

        // act
        var result = Executable.From(query).ToAsyncEnumerable();

        // assert
        await foreach (var item in result)
        {
            Assert.True(item is "a" or "b");
        }
    }

    [Fact]
    public static async Task ListAsyncEnumerable_ToAsyncEnumerable()
    {
        // arrange
        IQueryable<string> query = new ListAsyncEnumerable(["a", "b"]);

        // act
        var result = Executable.From(query).ToAsyncEnumerable();

        // assert
        await foreach (var item in result)
        {
            Assert.True(item is "a" or "b");
        }
    }

    [Fact]
    public static async Task List_WithSource()
    {
        // arrange
        var query = new[] { "a", "b" }.AsQueryable();
        var executableA = Executable.From(query);

        // act
        var executableB = executableA.WithSource(new[] { "a", "b", "c" }.AsQueryable());

        // assert
        Assert.NotEqual(executableA, executableB);

        Assert.Collection(
            await executableA.ToListAsync(),
            r => Assert.Equal("a", r),
            r => Assert.Equal("b", r));

        Assert.Collection(
            await executableB.ToListAsync(),
            r => Assert.Equal("a", r),
            r => Assert.Equal("b", r),
            r => Assert.Equal("c", r));
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
