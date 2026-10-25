using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public class NestedConnectionCostTests
{
    [Fact]
    public async Task Flat_Connection_Is_Priced_By_List_Size()
    {
        // arrange
        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    items(first: 200) {
                        nodes {
                            id
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
                .ModifyPagingOptions(o =>
                {
                    o.RequirePagingBoundaries = false;
                    o.MaxPageSize = 1000;
                })
                .ModifyCostOptions(o => o.EnforceCostLimits = false)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var response = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        // typeCost = 1 (query) + 1 (connection) + 1 * 200 (nodes) = 202.
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": []
                }
              },
              "extensions": {
                "operationCost": {
                  "fieldCost": 2,
                  "typeCost": 202
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Nested_Connection_Fan_Out_Is_Priced_Per_Level()
    {
        // arrange
        // each "nodes" response name repeats per nesting level; the cost of every level
        // must still be counted and multiplied by its own slicing argument.
        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    items(first: 200) {
                        nodes {
                            id
                            children(first: 100) {
                                nodes {
                                    id
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
                .ModifyPagingOptions(o =>
                {
                    o.RequirePagingBoundaries = false;
                    o.MaxPageSize = 1000;
                })
                .ModifyCostOptions(o => o.EnforceCostLimits = false)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var response = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        // typeCost = 1 (query) + 1 (outer connection) + ((1 + (1 + 1 * 100)) * 200) = 20402.
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": []
                }
              },
              "extensions": {
                "operationCost": {
                  "fieldCost": 402,
                  "typeCost": 20402
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Nested_Connection_Fan_Out_Is_Rejected_When_It_Exceeds_The_Type_Cost_Limit()
    {
        // arrange
        // a cost limit between the flat (~202) and nested (~20402) cost: the nested fan-out
        // must be rejected, proving the cost-limit guard is not bypassed.
        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    items(first: 200) {
                        nodes {
                            id
                            children(first: 100) {
                                nodes {
                                    id
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
                .ModifyPagingOptions(o =>
                {
                    o.RequirePagingBoundaries = false;
                    o.MaxPageSize = 1000;
                })
                .ModifyCostOptions(o =>
                {
                    o.EnforceCostLimits = true;
                    o.MaxTypeCost = 1000;
                    o.MaxFieldCost = 1000;
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var response = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed type cost was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "maxTypeCost": 1000,
                    "typeCost": 20402
                  }
                }
              ],
              "extensions": {
                "operationCost": {
                  "fieldCost": 402,
                  "typeCost": 20402
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Sibling_Connections_Are_Each_Priced()
    {
        // arrange
        // two aliased connections at the same depth both select "nodes"; the shared response
        // name must not let the first sibling claim it for the whole operation and skip the second.
        var operation =
            Utf8GraphQLParser.Parse(
                """
                {
                    a: items(first: 10) {
                        nodes {
                            id
                        }
                    }
                    b: items(first: 10) {
                        nodes {
                            id
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
                .ModifyPagingOptions(o =>
                {
                    o.RequirePagingBoundaries = false;
                    o.MaxPageSize = 1000;
                })
                .ModifyCostOptions(o => o.EnforceCostLimits = false)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var response = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        // typeCost = 1 (query) + 2 * (1 (connection) + 1 * 10 (nodes)) = 23.
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "nodes": []
                },
                "b": {
                  "nodes": []
                }
              },
              "extensions": {
                "operationCost": {
                  "fieldCost": 4,
                  "typeCost": 23
                }
              }
            }
            """);
    }

    public class Query
    {
        [UsePaging]
        public IEnumerable<Item> GetItems() => [];
    }

    public class Item
    {
        public required string Id { get; init; }

        [UsePaging]
        public IEnumerable<Item> GetChildren() => [];
    }
}
