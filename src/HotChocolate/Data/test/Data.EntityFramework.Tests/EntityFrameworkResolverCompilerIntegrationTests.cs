using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data;

public class EntityFrameworkResolverCompilerIntegrationTests
{
    [Fact]
    public async Task Resolver_Pipeline_With_DbContext_Is_Created()
    {
        Snapshot.FullName();

        using AuthorFixture authorFixture = new();

        var contextFactory = new Mock<IDbContextFactory<BookContext>>();
        contextFactory
            .Setup(t => t.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(authorFixture.Context));

        await new ServiceCollection()
            .AddSingleton(contextFactory.Object)
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPooledDbContext<BookContext>()
            .ExecuteRequestAsync("{ books { title } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Resolver_Pipeline_With_Request_DbContext_Is_Created()
    {
        Snapshot.FullName();

        using AuthorFixture authorFixture = new();

        using IServiceScope scope = new ServiceCollection()
            .AddScoped(_ => authorFixture.Context)
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddScopedDbContext<BookContext>(DbContextScope.Request)
            .Services
            .BuildServiceProvider()
            .CreateScope();

        await scope.ServiceProvider.ExecuteRequestAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ books { title } }")
                .SetServices(scope.ServiceProvider)
                .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Resolver_Pipeline_With_Field_DbContext_Is_Created()
    {
        Snapshot.FullName();

        using AuthorFixture authorFixture = new();

        await using ServiceProvider service = new ServiceCollection()
            .AddScoped(_ => authorFixture.Context)
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddScopedDbContext<BookContext>(DbContextScope.Field)
            .Services
            .BuildServiceProvider();

        await service.ExecuteRequestAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ books { title } }")
                .SetServices(service)
                .Create())
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public IQueryable<Book> GetBooks(BookContext context)
            => context.Books;
    }
}
