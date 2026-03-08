using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue6258ReproTests
{
    [Fact]
    public async Task Filter_For_Nested_Inherited_Id_Matches_Author_Id()
    {
        var executor = await CreateExecutorAsync();
        var result = await executor.ExecuteAsync(
            """
            {
              book(where: { authorId: { eq: 20 } }) {
                id
                author {
                  id
                }
              }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Filter_For_Nested_Inherited_Id_Does_Not_Match_Book_Id()
    {
        var executor = await CreateExecutorAsync();
        var result = await executor.ExecuteAsync(
            """
            {
              book(where: { authorId: { eq: 2 } }) {
                id
              }
            }
            """);

        result.MatchSnapshot();
    }

    public abstract class Issue6258Entity
    {
        public long Id { get; set; }
    }

    public class Issue6258Book : Issue6258Entity
    {
        public string Title { get; set; } = default!;

        public Issue6258Author Author { get; set; } = default!;
    }

    public class Issue6258Author : Issue6258Entity
    {
        public string Name { get; set; } = default!;
    }

    public sealed class Issue6258BookFilterInputType : FilterInputType<Issue6258Book>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Issue6258Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(b => b.Author.Id).Name("authorId");
        }
    }

    public class Issue6258Query
    {
        [UseFiltering<Issue6258BookFilterInputType>]
        public IQueryable<Issue6258Book> GetBook()
            => s_data.AsQueryable();
    }

    private static readonly Issue6258Book[] s_data =
    [
        new()
        {
            Id = 1,
            Title = "title 1",
            Author = new Issue6258Author { Id = 10, Name = "author 1" }
        },
        new()
        {
            Id = 2,
            Title = "title 2",
            Author = new Issue6258Author { Id = 20, Name = "author 2" }
        }
    ];

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddQueryType<Issue6258Query>()
            .BuildRequestExecutorAsync();
    }
}
