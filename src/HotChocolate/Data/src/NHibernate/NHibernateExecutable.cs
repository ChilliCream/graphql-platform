using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace HotChocolate.Data
{
    using NHibernate.Linq;

    public class NHibernateExecutable<T> : QueryableExecutable<T>
    {
        public NHibernateExecutable(IQueryable<T> queryable) : base(queryable)
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

    }
}
