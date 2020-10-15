using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public class Query
    {
        [UseDbContext(typeof(BookContext))]
        public IQueryable<Author> GetAuthors([ScopedService]BookContext context) =>
            context.Authors;

        [UseDbContext(typeof(BookContext))]
        public async Task<Author> GetAuthor([ScopedService]BookContext context) =>
            await context.Authors.FirstOrDefaultAsync();
    }
}
