using System.Linq;
using HotChocolate;
using HotChocolate.Data;

namespace Spatial.Demo
{
    public class Query
    {
        [UseDbContext(typeof(ApplicationDbContext))]
        [UseFiltering]
        public IQueryable<County> GetCounties(
            [ScopedService] ApplicationDbContext context) =>
            context.Counties;
    }
}
