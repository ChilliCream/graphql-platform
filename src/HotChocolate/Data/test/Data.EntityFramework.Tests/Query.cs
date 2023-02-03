#pragma warning disable CS0618
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Pagination.Extensions;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class Query
{
    [UseDbContext(typeof(BookContext))]
    public IQueryable<Author> GetAuthors(

        [ScopedService] BookContext context) =>
        context.Authors;

    [UseDbContext(typeof(BookContext))]
    public async Task<Author> GetAuthor(
        [ScopedService] BookContext context) =>
        (await context.Authors.FirstOrDefaultAsync())!;

    [UseDbContext(typeof(BookContext))]
    public Author? GetAuthorSync(
        [ScopedService] BookContext context) =>
        context.Authors.FirstOrDefault();

    [UseDbContext(typeof(BookContext))]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Author> GetAuthorOffsetPaging(
        [ScopedService] BookContext context) =>
        context.Authors;

    [UseDbContext(typeof(BookContext))]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IExecutable<Author> GetAuthorOffsetPagingExecutable(
        [ScopedService] BookContext context) =>
        context.Authors.AsExecutable();

    [UseDbContext(typeof(BookContext))]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IExecutable<Author> GetAuthorCursorPagingExecutable(
        [ScopedService] BookContext context) =>
        context.Authors.AsExecutable();

    [UseDbContext(typeof(BookContext))]
    [UsePaging(IncludeTotalCount = true)]
    public IQueryable<Author> GetAuthorCursorPaging(
        [ScopedService] BookContext context) =>
        context.Authors;

    [UseDbContext(typeof(BookContext))]
    [UsePaging(IncludeTotalCount = true)]
    public async Task<Connection<Author>> GetQueryableExtensionsCursor(
        [ScopedService] BookContext context,
        IResolverContext resolverContext,
        CancellationToken ct) =>
        await context.Authors
            .ApplyCursorPaginationAsync(resolverContext, cancellationToken: ct);

    [UseDbContext(typeof(BookContext))]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public async Task<CollectionSegment<Author>> GetQueryableExtensionsOffset(
        [ScopedService] BookContext context,
        IResolverContext resolverContext,
        CancellationToken ct) =>
        await context.Authors
            .ApplyOffsetPaginationAsync(resolverContext, cancellationToken: ct);
}

public class QueryTask
{
    [UseDbContext(typeof(BookContext))]
    public Task<IQueryable<Author>> GetAuthors(
        [ScopedService] BookContext context) =>
        Task.FromResult<IQueryable<Author>>(context.Authors);

    [UseDbContext(typeof(BookContext))]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<Author>> GetAuthorOffsetPaging(
        [ScopedService] BookContext context) =>
        Task.FromResult<IQueryable<Author>>(context.Authors);

    [UseDbContext(typeof(BookContext))]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
        [ScopedService] BookContext context) =>
        Task.FromResult(context.Authors.AsExecutable());

    [UseDbContext(typeof(BookContext))]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IExecutable<Author>> GetAuthorCursorPagingExecutable(
        [ScopedService] BookContext context) =>
        Task.FromResult(context.Authors.AsExecutable());

    [UseDbContext(typeof(BookContext))]
    [UsePaging(IncludeTotalCount = true)]
    public Task<IQueryable<Author>> GetAuthorCursorPaging(
        [ScopedService] BookContext context) =>
        Task.FromResult(context.Authors.AsQueryable());
}

public class QueryValueTask
{
    [UseDbContext(typeof(BookContext))]
    public ValueTask<IQueryable<Author>> GetAuthors(
        [ScopedService] BookContext context) =>
        new(context.Authors);

    [UseDbContext(typeof(BookContext))]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public ValueTask<IQueryable<Author>> GetAuthorOffsetPaging(
        [ScopedService] BookContext context) =>
        new(context.Authors);

    [UseDbContext(typeof(BookContext))]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public ValueTask<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
        [ScopedService] BookContext context) =>
        new(context.Authors.AsExecutable());

    [UseDbContext(typeof(BookContext))]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public ValueTask<IExecutable<Author>> GetAuthorCursorPagingExecutable(
        [ScopedService] BookContext context) =>
        new(context.Authors.AsExecutable());

    [UseDbContext(typeof(BookContext))]
    [UsePaging(IncludeTotalCount = true)]
    public ValueTask<IQueryable<Author>> GetAuthorCursorPaging(
        [ScopedService] BookContext context) =>
        new(context.Authors.AsQueryable());
}

public class InvalidQuery
{
    [UseDbContext(typeof(object))]
    public IQueryable<Author> GetAuthors([ScopedService] BookContext context) =>
        context.Authors;

    [UseDbContext(typeof(object))]
    public async Task<Author> GetAuthor([ScopedService] BookContext context) =>
        (await context.Authors.FirstOrDefaultAsync())!;
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(nameof(Query));

        descriptor
            .Field("books")
            .UseDbContext<BookContext>()
            .Resolve(ctx =>
            {
                var context = ctx.DbContext<BookContext>();

                return context.Books;
            });

        descriptor
            .Field("booksWithMissingContext")
            .Resolve(ctx =>
            {
                var context = ctx.DbContext<BookContext>();

                return context.Books;
            });
    }
}
#pragma warning restore CS0618
