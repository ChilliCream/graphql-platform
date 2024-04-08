using System.Collections;
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

    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await set.FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await set.FirstOrDefaultAsync(cancellationToken);

    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await set.SingleOrDefaultAsync(cancellationToken);

    public async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await set.SingleOrDefaultAsync(cancellationToken);


    public string Print() => set.ToString()!;
}
