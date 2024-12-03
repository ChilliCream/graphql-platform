using System.Text.Json;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public class PagingTests
{
    [Fact]
    public async Task Ensure_Paging_Defaults_Are_Applied()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Filtering_Not_Used()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    books {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = false)
                .AddFiltering()
                .AddSorting()
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        var expectation =
            JsonDocument.Parse(
                """
                {
                    "fieldCost": 6,
                    "typeCost": 52
                }
                """);

        await snapshot
            .Add(operation, "Operation")
            .Add(expectation.RootElement, "Expected")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Require_Paging_Boundaries_By_Default_With_Connections()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    books {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(operation, "Operation")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Require_Paging_Boundaries_Single_Boundary_With_Literal()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    books(first: 1) {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting()
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(operation, "Operation")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Require_Paging_Boundaries_Single_Boundary_With_Variable()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                query($first: Int) {
                    books(first: $first) {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting()
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(operation, "Operation")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Require_Paging_Boundaries_Two_Boundaries_With_Variable()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                query($first: Int, $last: Int) {
                    books(first: $first, last: $last) {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting()
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(operation, "Operation")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Require_Paging_Boundaries_Two_Boundaries_Mixed()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                query($first: Int) {
                    books(first: $first, last: 1) {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(operation, "Operation")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Require_Paging_Nested_Boundaries()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    books {
                        nodes {
                            title
                            authors {
                                nodes {
                                    name
                                }
                            }
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(operation, "Operation")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Filtering_Specific_Filter_Used()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    books(where: { title: { eq: "abc" } }) {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = false)
                .AddFiltering()
                .AddSorting()
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        var expectation =
            JsonDocument.Parse(
                """
                {
                    "fieldCost": 9,
                    "typeCost": 52
                }
                """);

        await snapshot
            .Add(operation, "Operation")
            .Add(expectation.RootElement, "Expected")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Filtering_Specific_Expensive_Filter_Used()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    books(where: { title: { contains: "abc" } }) {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = false)
                .AddFiltering()
                .AddSorting()
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        var expectation =
            JsonDocument.Parse(
                """
                {
                    "fieldCost": 10,
                    "typeCost": 52
                }
                """);

        await snapshot
            .Add(operation, "Operation")
            .Add(expectation.RootElement, "Expected")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Filtering_Variable()
    {
        // arrange
        var snapshot = new Snapshot();

        var operation =
            Utf8GraphQLParser.Parse(
                """
                query($where: BookFilterInput){
                    books(where: $where) {
                        nodes {
                            title
                        }
                    }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = false)
                .AddFiltering()
                .AddSorting()
                .BuildRequestExecutorAsync();

        // act
        var response = await executor.ExecuteAsync(request);

        // assert
        var expectation =
            JsonDocument.Parse(
                """
                {
                    "fieldCost": 10,
                    "typeCost": 52
                }
                """);

        await snapshot
            .Add(operation, "Operation")
            .Add(expectation.RootElement, "Expected")
            .Add(response, "Response")
            .MatchMarkdownAsync();
    }

    public class Query
    {
        [UsePaging]
        [UseFiltering<BookFilterInputType>]
        [UseSorting<BookSortInputType>]
        public IQueryable<Book> GetBooks() => new List<Book>().AsQueryable();

        [UsePaging(ConnectionName = "BooksTotal", IncludeTotalCount = true)]
        [UseFiltering<BookFilterInputType>]
        [UseSorting<BookSortInputType>]
        public IQueryable<Book> GetBooksWithTotalCount() => new List<Book>().AsQueryable();

        [UseOffsetPaging]
        [UseFiltering<BookFilterInputType>]
        [UseSorting<BookSortInputType>]
        public IQueryable<Book> GetBooksOffset() => new List<Book>().AsQueryable();

        [UseOffsetPaging(CollectionSegmentName = "BooksTotal", IncludeTotalCount = true)]
        [UseFiltering<BookFilterInputType>]
        [UseSorting<BookSortInputType>]
        public IQueryable<Book> GetBooksOffsetWithTotalCount() => new List<Book>().AsQueryable();
    }

    public class Book
    {
        public required string Title { get; set; }

        [UsePaging] public List<Author> Authors => new();
    }

    public class Author
    {
        public required string Name { get; set; }
    }

    public class BookFilterInputType : FilterInputType<Book>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Title);
        }
    }

    public class BookSortInputType : SortInputType<Book>
    {
        protected override void Configure(ISortInputTypeDescriptor<Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Title);
        }
    }
}
