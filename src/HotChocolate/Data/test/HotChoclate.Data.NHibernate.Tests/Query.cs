using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;
using NHibernate;
using NHibernate.Linq;

namespace HotChocolate.Data
{
    public class Query
    {
        [UseNHibernateSession]
        public IQueryable<Author> GetAuthors(
            [ScopedService]ISession session) =>
            session.Query<Author>();

        [UseNHibernateSession]
        public async Task<Author> GetAuthor(
            [ScopedService] ISession session) =>
            await session.Query<Author>().FirstOrDefaultAsync();

        [UseNHibernateSession]
        public Author? GetAuthorSync(
            [ScopedService] ISession session) =>
            session.Query<Author>().FirstOrDefault();

        [UseNHibernateSession]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Author> GetAuthorOffsetPaging(
            [ScopedService] ISession session) =>
            session.Query<Author>();

        [UseNHibernateSession]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IExecutable<Author> GetAuthorOffsetPagingExecutable(
            [ScopedService] ISession session) =>
            session.Query<Author>().AsExecutable();

        [UseNHibernateSession]
        [UsePaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IExecutable<Author> GetAuthorCursorPagingExecutable(
            [ScopedService] ISession session) =>
            session.Query<Author>().AsExecutable();
    }

    public class QueryTask
    {
        [UseNHibernateSession]
        public Task<IQueryable<Author>> GetAuthors(
            [ScopedService] ISession session) =>
            Task.FromResult<IQueryable<Author>>(session.Query<Author>());

        [UseNHibernateSession]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public Task<IQueryable<Author>> GetAuthorOffsetPaging(
            [ScopedService] ISession session) =>
            Task.FromResult<IQueryable<Author>>(session.Query<Author>());

        [UseNHibernateSession]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public Task<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
            [ScopedService] ISession session) =>
            Task.FromResult(session.Query<Author>().AsNhibernateExecutable());

        [UseNHibernateSession]
        [UsePaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public Task<IExecutable<Author>> GetAuthorCursorPagingExecutable(
            [ScopedService] ISession session) =>
            Task.FromResult(session.Query<Author>().AsNhibernateExecutable());
    }

    public class QueryValueTask
    {
        [UseNHibernateSession]
        public ValueTask<IQueryable<Author>> GetAuthors(
            [ScopedService] ISession session) =>
            new(session.Query<Author>());

        [UseNHibernateSession]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public ValueTask<IQueryable<Author>> GetAuthorOffsetPaging(
            [ScopedService] ISession session) =>
            new(session.Query<Author>());

        [UseNHibernateSession]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public ValueTask<IExecutable<Author>> GetAuthorOffsetPagingExecutable(
            [ScopedService] ISession session) =>
            new(session.Query<Author>().AsExecutable());

        [UseNHibernateSession]
        [UsePaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public ValueTask<IExecutable<Author>> GetAuthorCursorPagingExecutable(
            [ScopedService] ISession session) =>
            new(session.Query<Author>().AsExecutable());
    }

    public class InvalidQuery
    {
        [UseNHibernateSession]
        public IQueryable<Author> GetAuthors([ScopedService] ISession session) =>
            session.Query<Author>();

        [UseNHibernateSession]
        public async Task<Author> GetAuthor([ScopedService] ISession session) =>
            await session.Query<Author>().FirstOrDefaultAsync();
    }
}
