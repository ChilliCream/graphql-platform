using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion;

public class SemanticNonNullTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Nullable_Field_Null_Is_Returned_Without_Error()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String @null
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "field": null
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task Null_Field_Without_Error_Produces_Error_And_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String @semanticNonNull @null
            }
            """,
            enableSemanticNonNull: true);;

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for non-nullable field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "field"
                                         ],
                                         "extensions": {
                                           "code": "HC0018"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "field": null
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task Field_With_Error_Is_Just_Nulled()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String @semanticNonNull @error
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Unexpected Execution Error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "field"
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "field": null
                                     }
                                   }
                                   """);
    }
}
