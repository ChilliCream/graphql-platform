using System.Linq;
using HotChocolate;
using HotChocolate.Data;

namespace Spatial.Demo
{
    public class Query
    {
        [UseApplicationDbContext]
        [ToListAsync]
        [UseFiltering]
        public IQueryable<County> GetCounties([ScopedService] ApplicationDbContext context)
            => context.Counties;
    }
}
