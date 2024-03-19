using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Data;

public class EntityFrameworkResolverCompilerIntegrationTests
{
    [Fact]
    public async Task DBContext_On_Queries_Is_Scoped()
    {
        using AuthorFixture authorFixture = new();

        await using var service = new ServiceCollection()
            .AddScoped(_ => authorFixture.Context)
            .AddGraphQL()
            .AddQueryType<Query>()
            .Services
            .BuildServiceProvider();

        var result = await service.ExecuteRequestAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ books { title } }")
                .SetServices(service)
                .Create());

        result.MatchSnapshot();
    }

    public class Query
    {
        public IQueryable<Book> GetBooks(BookContext context)
            => context.Books;
    }
}
