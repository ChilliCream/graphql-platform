using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public class Query
    {
        [UseDbContext(typeof(BookContext))]
        public IQueryable<Author> GetAuthors(
            [ScopedService]BookContext context) =>
            context.Authors;

        [UseDbContext(typeof(BookContext))]
        public async Task<Author> GetAuthor(
            [ScopedService]BookContext context) =>
            await context.Authors.FirstOrDefaultAsync();

        [UseDbContext(typeof(BookContext))]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Author> GetAuthorOffsetPaging(
            [ScopedService]BookContext context) =>
            context.Authors;

        [UseDbContext(typeof(BookContext))]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IExecutable<Author> GetAuthorOffsetPagingExecutable(
            [ScopedService]BookContext context) =>
            context.Authors.AsExecutable();

        [UseDbContext(typeof(BookContext))]
        [UsePaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IExecutable<Author> GetAuthorCursorPagingExecutable(
            [ScopedService]BookContext context) =>
            context.Authors.AsExecutable();
    }

    public class QueryTask
    {
        [UseDbContext(typeof(BookContext))]
        public Task<IQueryable<Author>> GetAuthors(
            [ScopedService]BookContext context) =>
            Task.FromResult<IQueryable<Author>>(context.Authors);

        [UseDbContext(typeof(BookContext))]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public Task<IQueryable<Author>> GetAuthorOffsetPaging(
            [ScopedService]BookContext context) =>
            Task.FromResult<IQueryable<Author>>(context.Authors);

        [UseDbContext(typeof(BookContext))]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public Task<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
            [ScopedService]BookContext context) =>
            Task.FromResult(context.Authors.AsExecutable());

        [UseDbContext(typeof(BookContext))]
        [UsePaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public Task<IExecutable<Author>> GetAuthorCursorPagingExecutable(
            [ScopedService]BookContext context) =>
            Task.FromResult(context.Authors.AsExecutable());
    }

    public class QueryValueTask
    {
        [UseDbContext(typeof(BookContext))]
        public ValueTask<IQueryable<Author>> GetAuthors(
            [ScopedService]BookContext context) =>
            new(context.Authors);

        [UseDbContext(typeof(BookContext))]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public ValueTask<IQueryable<Author>> GetAuthorOffsetPaging(
            [ScopedService]BookContext context) =>
            new(context.Authors);

        [UseDbContext(typeof(BookContext))]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public ValueTask<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
            [ScopedService]BookContext context) =>
            new(context.Authors.AsExecutable());

        [UseDbContext(typeof(BookContext))]
        [UsePaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public ValueTask<IExecutable<Author>> GetAuthorCursorPagingExecutable(
            [ScopedService]BookContext context) =>
            new(context.Authors.AsExecutable());
    }

    public class InvalidQuery
    {
        [UseDbContext(typeof(object))]
        public IQueryable<Author> GetAuthors([ScopedService]BookContext context) =>
            context.Authors;

        [UseDbContext(typeof(object))]
        public async Task<Author> GetAuthor([ScopedService]BookContext context) =>
            await context.Authors.FirstOrDefaultAsync();
    }
}
