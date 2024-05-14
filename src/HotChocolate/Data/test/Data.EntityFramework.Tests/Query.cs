#pragma warning disable CS0618
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class Query
{
    public IQueryable<Author> GetAuthors(
        BookContext context) =>
        context.Authors;

    public async Task<Author> GetAuthor(
        BookContext context) =>
        (await context.Authors.FirstOrDefaultAsync())!;
    
    public Author? GetAuthorSync(
        BookContext context) =>
        context.Authors.FirstOrDefault();
    
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Author> GetAuthorOffsetPaging(
        BookContext context) =>
        context.Authors;

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IExecutable<Author> GetAuthorOffsetPagingExecutable(
        BookContext context) =>
        context.Authors.AsExecutable();

    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IExecutable<Author> GetAuthorCursorPagingExecutable(
        BookContext context) =>
        context.Authors.AsExecutable();
    
    [UsePaging(IncludeTotalCount = true)]
    public IQueryable<Author> GetAuthorCursorPaging(
        BookContext context) =>
        context.Authors;
    
    [UsePaging(IncludeTotalCount = true)]
    public async Task<Connection<Author>> GetQueryableExtensionsCursor(
        BookContext context,
        IResolverContext resolverContext,
        CancellationToken ct) 
        => await context.Authors.ApplyCursorPaginationAsync(resolverContext, cancellationToken: ct);
    
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public async Task<CollectionSegment<Author>> GetQueryableExtensionsOffset(
        BookContext context,
        IResolverContext resolverContext,
        CancellationToken ct) 
        => await context.Authors.ApplyOffsetPaginationAsync(resolverContext, cancellationToken: ct);
}

public class QueryTask
{
    public Task<IQueryable<Author>> GetAuthors(
        BookContext context) 
        => Task.FromResult<IQueryable<Author>>(context.Authors);
    
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<Author>> GetAuthorOffsetPaging(
        BookContext context) 
        => Task.FromResult<IQueryable<Author>>(context.Authors);
    
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
        BookContext context) 
        => Task.FromResult(context.Authors.AsExecutable());
    
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IExecutable<Author>> GetAuthorCursorPagingExecutable(
        BookContext context) 
        => Task.FromResult(context.Authors.AsExecutable());
    
    [UsePaging(IncludeTotalCount = true)]
    public Task<IQueryable<Author>> GetAuthorCursorPaging(
        BookContext context) 
        => Task.FromResult(context.Authors.AsQueryable());
}

public class QueryValueTask
{
    public ValueTask<IQueryable<Author>> GetAuthors(
        BookContext context) 
        => new(context.Authors);
    
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public ValueTask<IQueryable<Author>> GetAuthorOffsetPaging(
        BookContext context) 
        => new(context.Authors);
    
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public ValueTask<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
        BookContext context) 
        => new(context.Authors.AsExecutable());
    
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public ValueTask<IExecutable<Author>> GetAuthorCursorPagingExecutable(
        BookContext context) 
        => new(context.Authors.AsExecutable());
    
    [UsePaging(IncludeTotalCount = true)]
    public ValueTask<IQueryable<Author>> GetAuthorCursorPaging(
        BookContext context) 
        => new(context.Authors.AsQueryable());
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(nameof(Query));

        descriptor
            .Field("books")
            .Resolve(ctx =>
            {
                var context = ctx.Service<BookContext>();
                return context.Books;
            });
    }
}
#pragma warning restore CS0618
