using System.Collections;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class EntityFrameworkExecutable<T>(IQueryable<T> queryable) : QueryableExecutable<T>(queryable)
{
    public override async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
        => await Source.ToListAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<T?> FirstOrDefaultAsync(
        CancellationToken cancellationToken)
        => await Source.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<T?> SingleOrDefaultAsync(
        CancellationToken cancellationToken)
        => await Source.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override string Print() => Source.ToQueryString();
}
