using System.Collections;
using System.Runtime.CompilerServices;

namespace HotChocolate.Types.Pagination;

public class MockExecutable<T>(IQueryable<T> source) : IExecutable<T>
{
    public object Source => source;

    public ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        => new(source.ToList());

    ValueTask<List<T>> IExecutable<T>.ToListAsync(CancellationToken cancellationToken)
        => new(source.ToList());

    public async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryable = await new ValueTask<IQueryable<T>>(source);

        foreach (var item in queryable)
        {
            yield return item;
        }
    }

    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryable = await new ValueTask<IQueryable<T>>(source);

        foreach (var item in queryable)
        {
            yield return item;
        }
    }

    public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.FirstOrDefault());

    ValueTask<T?> IExecutable<T>.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.FirstOrDefault());

    public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.SingleOrDefault());

    ValueTask<T?> IExecutable<T>.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.SingleOrDefault());

    public string Print()
        => source.ToString()!;
}
