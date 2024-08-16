using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

public static class DisposeExecutablesTests
{
    [Fact]
    public static async Task Executable_DisposeAsync()
    {
        // arrange
        var executable = new AsyncDisposableExecutable(["a", "b", "c"]);

        // act
        await using var result =
            await new ServiceCollection()
                .AddSingleton<IExecutable<string>>(executable)
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    {
                        items
                    }
                    """);

        // assert
        Assert.True(executable.IsDisposed);
    }

    [Fact]
    public static async Task Executable_Dispose()
    {
        // arrange
        var executable = new DisposableExecutable(["a", "b", "c"]);

        // act
        await using var result =
            await new ServiceCollection()
                .AddSingleton<IExecutable<string>>(executable)
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    {
                        items
                    }
                    """);

        // assert
        Assert.True(executable.IsDisposed);
    }

    public class Query
    {
        public IExecutable<string> Items(IExecutable<string> result) => result;
    }

    private class AsyncDisposableExecutable(string[] items) : Executable<string>, IAsyncDisposable
    {
        public override object Source => items;

        public bool IsDisposed { get; private set; }

        public override IAsyncEnumerable<string> ToAsyncEnumerable(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<string?> FirstOrDefaultAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<string?> SingleOrDefaultAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<int> CountAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<List<string>> ToListAsync(CancellationToken cancellationToken = default)
            => new(items.ToList());

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }

    private class DisposableExecutable(string[] items) : Executable<string>, IDisposable
    {
        public override object Source => items;

        public bool IsDisposed { get; private set; }

        public override IAsyncEnumerable<string> ToAsyncEnumerable(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<string?> FirstOrDefaultAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<string?> SingleOrDefaultAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<int> CountAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask<List<string>> ToListAsync(
            CancellationToken cancellationToken = default)
            => new(items.ToList());

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
