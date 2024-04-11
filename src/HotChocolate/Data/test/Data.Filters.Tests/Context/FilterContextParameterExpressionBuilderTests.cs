using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class FilterContextParameterExpressionBuilderTests
{
    [Fact]
    public async Task Should_InjectFilterContext()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                books(where: { title: { eq: ""test"" } }) {
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
        public static IFilterContext? Context { get; private set; }

        [UseFiltering]
        public IEnumerable<Book> Books(IFilterContext context)
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
