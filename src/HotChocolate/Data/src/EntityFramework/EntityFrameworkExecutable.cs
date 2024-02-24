using System.Collections;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class EntityFrameworkExecutable<T>(IQueryable<T> queryable) : QueryableExecutable<T>(queryable)
{
    public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        => await Source.ToListAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<object?> FirstOrDefaultAsync(
        CancellationToken cancellationToken)
        => await Source.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<object?> SingleOrDefaultAsync(
        CancellationToken cancellationToken) 
        => await Source.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override string Print() => Source.ToQueryString();
}
