using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public class SlicingArgumentsTests
{
    [Fact]
    public async Task SlicingArguments_Non_Specified()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .ExecuteRequestAsync(
                    """
                    {
                        foos {
                            nodes
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Exactly one slicing argument must be defined.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 5
                    }
                  ],
                  "path": [
                    "foos"
                  ],
                  "extensions": {
                    "code": "HC0082"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task SlicingArguments_Set_To_Null()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .ExecuteRequestAsync(
                    """
                    {
                        foos(first: null) {
                            nodes
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Exactly one slicing argument must be defined.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 5
                    }
                  ],
                  "path": [
                    "foos"
                  ],
                  "extensions": {
                    "code": "HC0082"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task SlicingArguments_Set_First_To_Null_And_Last_To_Null()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .ExecuteRequestAsync(
                    """
                    {
                        foos(first: null last: null) {
                            nodes
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Exactly one slicing argument must be defined.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 5
                    }
                  ],
                  "path": [
                    "foos"
                  ],
                  "extensions": {
                    "code": "HC0082"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task SlicingArguments_First_Is_Null_And_Last_Is_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    {
                        foos(first: null, last: 1) {
                            nodes
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foos": {
                  "nodes": [
                    100
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task SlicingArguments_First_Is_1_And_Last_Is_Null()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    {
                        foos(first: 1, last: null) {
                            nodes
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foos": {
                  "nodes": [
                    1
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task SlicingArguments_First_Is_Variable_And_Last_Is_Null()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query($first: Int = 1) {
                        foos(first: $first, last: null) {
                            nodes
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foos": {
                  "nodes": [
                    1
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task SlicingArguments_First_Is_Variable_And_Last_Is_Variable()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query($first: Int = 1, $last: Int = null) {
                        foos(first: $first, last: $last) {
                            nodes
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foos": {
                  "nodes": [
                    1
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task SlicingArgumentDefaultValue_Inferred_From_DefaultPageSize()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query2>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SlicingArgumentDefaultValue_ListSizeAttribute_HasPrecedenceOver_DefaultPageSize()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query3>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    public class Query
    {
        [UsePaging]
        public IEnumerable<int> GetFoos() => Enumerable.Range(1, 100);
    }

    public class Query2
    {
        [UsePaging(DefaultPageSize = 42)]
        public IEnumerable<int> GetFoos() => Enumerable.Range(1, 100);
    }

    public class Query3
    {
        [UsePaging(DefaultPageSize = 42)]
        [ListSize(
            AssumedSize = 10,
            SlicingArguments = ["first", "last"],
            SizedFields = ["edges", "nodes"],
            RequireOneSlicingArgument = false,
            SlicingArgumentDefaultValue = 999)]
        public IEnumerable<int> GetFoos() => Enumerable.Range(1, 100);
    }
}
