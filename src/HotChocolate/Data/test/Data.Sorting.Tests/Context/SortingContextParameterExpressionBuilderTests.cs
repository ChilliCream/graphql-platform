using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class SortingContextParameterExpressionBuilderTests
{
    [Fact]
    public async Task Should_InjectSortingContext()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                books(order: { title: DESC }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(Query.Context);
    }

    public class Query
    {
        public static ISortingContext? Context { get; private set; }

        [UseSorting]
        public IEnumerable<Book> Books(ISortingContext context)
        {
            Context = context;

            return Array.Empty<Book>();
        }
    }

    public class Book
    {
        public int Id { get; set; }

        public string? Title { get; set; }
    }
}
