using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public class MockExecutable<T> : IExecutable<T>
        where T : class
    {
        private readonly DbSet<T> _dbSet;

        public MockExecutable(DbSet<T> dbSet)
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
