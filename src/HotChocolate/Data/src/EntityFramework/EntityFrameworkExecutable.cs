using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public class EntityFrameworkExecutable<T> : QueryableExecutable<T>
    {
        public EntityFrameworkExecutable(IQueryable<T> queryable) : base(queryable)
        {
        }

        public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken) =>
            await Source.ToListAsync(cancellationToken).ConfigureAwait(false);

        public override async ValueTask<object?> FirstOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await Source.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        public override async ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await Source.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        public override string Print() => Source.ToQueryString();
    }
}
