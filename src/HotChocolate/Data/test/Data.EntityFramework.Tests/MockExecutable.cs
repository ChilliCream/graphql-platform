using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class MockExecutable<T>(IQueryable<T> set) : IExecutable<T>
    where T : class
{
    public object Source => set;

    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await set.ToListAsync(cancellationToken);

    public async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
        => await set.ToListAsync(cancellationToken);

    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach(var item in ToAsyncEnumerable(cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if(set is IAsyncEnumerable<T> asyncEnumerable)
        {
            await foreach(var item in asyncEnumerable.WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
        else
        {
            foreach(var item in set)
            {
                yield return item;
            }
        }
    }

    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await set.FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await set.FirstOrDefaultAsync(cancellationToken);

    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await set.SingleOrDefaultAsync(cancellationToken);

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await set.SingleOrDefaultAsync(cancellationToken);

    public string Print() => set.ToString()!;
}
