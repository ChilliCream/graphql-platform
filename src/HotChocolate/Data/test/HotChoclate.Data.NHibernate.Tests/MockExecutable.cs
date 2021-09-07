using System.Collections;
using System.Threading;
using System.Threading.Tasks;


namespace HotChocolate.Data
{
    using System.Linq;
    using NHibernate.Linq;

    public class MockExecutable<T> : IExecutable<T>
        where T : class
    {
        private readonly IQueryable<T> _dbSet;

        public MockExecutable(IQueryable<T> dbSet)
        {
            _dbSet = dbSet;
        }

        public object Source => _dbSet;

        public async ValueTask<IList> ToListAsync(CancellationToken cancellationToken) =>
            await _dbSet.ToListAsync(cancellationToken);

        public async ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken) =>
            await _dbSet.FirstOrDefaultAsync(cancellationToken);

        public async ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken) =>
            await _dbSet.SingleOrDefaultAsync(cancellationToken);

        public string Print() => _dbSet.ToString();
    }
}
