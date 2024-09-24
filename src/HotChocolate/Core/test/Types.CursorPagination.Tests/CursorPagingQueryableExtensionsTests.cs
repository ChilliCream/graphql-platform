using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Types.Pagination;

public class CursorPagingQueryableExtensionsTests
{
    [Fact]
    public async Task Queryable_Query_Is_Null()
    {
        var mock = new Mock<IResolverContext>();

        async Task Fail()
            => await default(IQueryable<Person>)!.ApplyCursorPaginationAsync(mock.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Queryable_Context_Is_Null()
    {
        var mock = new Mock<IQueryable<Person>>();

        async Task Fail() => await mock.Object.ApplyCursorPaginationAsync(default!);

        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Queryable_ApplyCursorPaginationAsync_No_Boundaries()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync("{ persons { nodes { name } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Queryable_ApplyCursorPaginationAsync_First_1()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync("{ persons(first: 1) { nodes { name } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Enumerable_Query_Is_Null()
    {
        var mock = new Mock<IResolverContext>();

        async Task Fail()
            => await default(IEnumerable<Person>)!.ApplyCursorPaginationAsync(mock.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Enumerable_Context_Is_Null()
    {
        var mock = new Mock<IEnumerable<Person>>();

        async Task Fail() => await mock.Object.ApplyCursorPaginationAsync(default!);

        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Enumerable_ApplyCursorPaginationAsync_No_Boundaries()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryEnumerable>()
            .ExecuteRequestAsync("{ persons { nodes { name } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Enumerable_ApplyCursorPaginationAsync_First_1()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryEnumerable>()
            .ExecuteRequestAsync("{ persons(first: 1) { nodes { name } } }")
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [UsePaging]
        public async Task<Connection<Person>> GetPersons(
            IResolverContext context,
            CancellationToken cancellationToken)
        {
            var list = new Person[]
            {
                new(name: "Foo"),
                new(name: "Bar"),
                new(name: "Baz"),
                new(name: "Qux"),
            };

            return await list.AsQueryable().ApplyCursorPaginationAsync(
                context,
                defaultPageSize: 2);
        }
    }

    public class QueryEnumerable
    {
        [UsePaging]
        public async Task<Connection<Person>> GetPersons(
            IResolverContext context,
            CancellationToken cancellationToken)
        {
            var list = new Person[]
            {
                new(name: "Foo"),
                new(name: "Bar"),
                new(name: "Baz"),
                new(name: "Qux"),
            };

            return await list.ApplyCursorPaginationAsync(
                context,
                defaultPageSize: 2);
        }
    }

    public class Person(string name)
    {
        public string Name { get; set; } = name;
    }
}
