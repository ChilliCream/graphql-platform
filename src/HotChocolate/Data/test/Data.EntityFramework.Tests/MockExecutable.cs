using System.Collections.Generic;
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

        async ValueTask<object> IExecutable.ExecuteAsync(CancellationToken cancellationToken)
        {
            return await ExecuteAsync(cancellationToken);
        }

        public async ValueTask<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        public string Print()
        {
            throw new System.NotImplementedException();
        }
    }
}
