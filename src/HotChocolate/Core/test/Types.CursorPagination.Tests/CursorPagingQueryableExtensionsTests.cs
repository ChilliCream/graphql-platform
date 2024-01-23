using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

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

        async Task Fail()
            => await mock.Object.ApplyCursorPaginationAsync(default(IResolverContext)!);

        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Queryable_ApplyCursorPaginationAsync_No_Boundaries()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync("{ persons { nodes { name } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Queryable_ApplyCursorPaginationAsync_First_1()
    {
        Snapshot.FullName();

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

        async Task Fail()
            => await mock.Object.ApplyCursorPaginationAsync(default(IResolverContext)!);

        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Enumerable_ApplyCursorPaginationAsync_No_Boundaries()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryEnumerable>()
            .ExecuteRequestAsync("{ persons { nodes { name } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Enumerable_ApplyCursorPaginationAsync_First_1()
    {
        Snapshot.FullName();

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
                new() { Name = "Foo", },
                new() { Name = "Bar", },
                new() { Name = "Baz", },
                new() { Name = "Qux", },
            };

            return await list.AsQueryable().ApplyCursorPaginationAsync(
                context,
                defaultPageSize: 2,
                totalCount: list.Length,
                cancellationToken: cancellationToken);
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
                new() { Name = "Foo", },
                new() { Name = "Bar", },
                new() { Name = "Baz", },
                new() { Name = "Qux", },
            };

            return await list.ApplyCursorPaginationAsync(
                context,
                defaultPageSize: 2,
                totalCount: list.Length,
                cancellationToken: cancellationToken);
        }
    }

    public class Person
    {
        public string Name { get; set; }
    }
}
