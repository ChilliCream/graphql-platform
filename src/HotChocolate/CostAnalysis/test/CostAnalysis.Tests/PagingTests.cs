using System.Text.Json;
using CookieCrumble;
using HotChocolate.Data;
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
            OperationRequestBuilder.Create()
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
            OperationRequestBuilder.Create()
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
            OperationRequestBuilder.Create()
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
            OperationRequestBuilder.Create()
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
        [UseFiltering]
        [UseSorting]
        public IQueryable<Book> GetBooks() => new List<Book>().AsQueryable();
    }

    public class Book
    {
        public required string Title { get; set; }
    }
}
