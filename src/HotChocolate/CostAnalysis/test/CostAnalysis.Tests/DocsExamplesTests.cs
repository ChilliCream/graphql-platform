using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Runs the worked examples from the "How Cost Is Calculated" section of the cost-analysis docs
/// page in report mode and snapshots the reported field cost and type cost. The docs numbers are
/// copied from these snapshots, so a change in what the analyzer reports for these queries fails
/// this test before the docs go stale.
/// </summary>
public sealed class DocsExamplesTests
{
    [Fact]
    public async Task HowCostIsCalculatedDocs_FieldCostExample_Should_ReportDocumentedCost()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        book {
                            title
                            author {
                                name
                            }
                        }
                    }
                    """)
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "book": {
                  "title": "C# in Depth",
                  "author": {
                    "name": "Jon Skeet"
                  }
                }
              },
              "extensions": {
                "operationCost": {
                  "fieldCost": 11,
                  "typeCost": 3
                }
              }
            }
            """);
    }

    [Fact]
    public async Task HowCostIsCalculatedDocs_PaginatedExample_Should_ReportDocumentedCost()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        books(first: 50) {
                            edges {
                                node {
                                    title
                                    author {
                                        name
                                    }
                                }
                            }
                        }
                    }
                    """)
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": {
                  "edges": []
                }
              },
              "extensions": {
                "operationCost": {
                  "fieldCost": 111,
                  "typeCost": 152
                }
              }
            }
            """);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .ModifyPagingOptions(o => o.RequirePagingBoundaries = false);

    public class Query
    {
        public async Task<Book?> GetBookAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new Book { Title = "C# in Depth", Author = new Author { Name = "Jon Skeet" } };
        }

        [UsePaging]
        public async Task<IEnumerable<Book>> GetBooksAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            return [];
        }
    }

    public class Book
    {
        public required string Title { get; init; }

        public required Author Author { get; init; }
    }

    public class Author
    {
        public required string Name { get; init; }
    }
}
