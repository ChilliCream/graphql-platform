using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Data.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Sorting_Should_Work_When_UsedWithNonNullDateTime()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        const string query = @"
        {
            foos(order: { createdUtc: DESC }) {
                createdUtc
            }
        }
        ";

        // act
        IExecutionResult result = await executor.ExecuteAsync(query);

        // assert
        result.MatchSnapshot();
    }
}

public class Query
{
    [UseSorting]
    public IEnumerable<Foo> Foos() => new[]
    {
        new Foo { CreatedUtc = new DateTime(2000, 1, 1, 1, 1, 1) },
        new Foo { CreatedUtc = new DateTime(2010, 1, 1, 1, 1, 1) },
        new Foo { CreatedUtc = new DateTime(2020, 1, 1, 1, 1, 1) }
    };
}

public class Foo
{
    [GraphQLType(typeof(NonNullType<DateType>))]
    public DateTime CreatedUtc { get; set; }
}
