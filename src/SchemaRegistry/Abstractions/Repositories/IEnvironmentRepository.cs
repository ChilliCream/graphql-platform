using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MarshmallowPie.Repositories
{
    public interface IEnvironmentRepository
    {
        Task AddEnvironmentAsync(Environment environment);

        Task UpdateEnvironmentAsync(Environment environment);

        Task<Environment> GetEnvironmentAsync(Guid id);

        IQueryable<Environment> GetEnvironments();
    }
}
