using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion;

public class SemanticNonNullTests(ITestOutputHelper output)
{
    # region Scalar

    [Fact]
    public async Task Nullable_Scalar_Field_Subgraph_Returns_Null_Gateway_Nulls_Field()
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
    public async Task SemanticNonNull_Scalar_Field_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         field: String @semanticNonNull
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["field"] = null })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);
        ;

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
    public async Task SemanticNonNull_Scalar_Field_Subgraph_Returns_Error_For_Field_Gateway_Nulls_Only_Nulls_Field()
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

    #endregion

    #region Enum

    [Fact]
    public async Task Nullable_Enum_Field_Subgraph_Returns_Null_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyEnum @null
            }

            enum MyEnum {
              VALUE
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
    public async Task SemanticNonNull_Enum_Field_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         field: MyEnum @semanticNonNull
                                       }

                                       enum MyEnum {
                                         VALUE
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["field"] = null })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);
        ;

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
    public async Task SemanticNonNull_Enum_Field_Subgraph_Returns_Error_For_Field_Gateway_Nulls_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyEnum @semanticNonNull @error
            }

            enum MyEnum {
              VALUE
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

    #endregion

    #region Object

    [Fact]
    public async Task Nullable_Object_Field_Subgraph_Returns_Null_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyObject @null
            }

            type MyObject {
              anotherField: String
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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
    public async Task SemanticNonNull_Object_Field_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         field: MyObject @semanticNonNull
                                       }

                                       type MyObject {
                                         anotherField: String
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["field"] = null })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);
        ;

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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
    public async Task SemanticNonNull_Object_Field_Subgraph_Returns_Error_For_Field_Gateway_Nulls_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyObject @semanticNonNull @error
            }

            type MyObject {
              anotherField: String
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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

    #endregion

    #region Field on Composite Type

    [Fact]
    public async Task Nullable_Scalar_Field_On_Object_Subgraph_Returns_Null_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyObject
            }

            type MyObject {
              anotherField: String @null
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "field": {
                                         "anotherField": null
                                       }
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task SemanticNonNull_Scalar_Field_On_Object_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         field: MyObject
                                       }

                                       type MyObject {
                                         anotherField: String @semanticNonNull
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?>
                        {
                            ["field"] = new Dictionary<string, object?>
                            {
                                ["anotherField"] = null
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);
        ;

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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
                                             "line": 3,
                                             "column": 5
                                           }
                                         ],
                                         "path": [
                                           "field",
                                           "anotherField"
                                         ],
                                         "extensions": {
                                           "code": "HC0018"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "field": {
                                         "anotherField": null
                                       }
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        SemanticNonNull_Scalar_Field_On_Object_Subgraph_Returns_Error_For_Field_Gateway_Nulls_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyObject
            }

            type MyObject {
              anotherField: String @semanticNonNull @error
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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
                                             "line": 3,
                                             "column": 5
                                           }
                                         ],
                                         "path": [
                                           "field",
                                           "anotherField"
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "field": {
                                         "anotherField": null
                                       }
                                     }
                                   }
                                   """);
    }

    #endregion

    #region Interface

    [Fact]
    public async Task Nullable_Interface_Field_Subgraph_Returns_Null_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyInterface @null
            }

            interface MyInterface {
              anotherField: String
            }

            type MyObject implements MyInterface {
              anotherField: String
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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
    public async Task SemanticNonNull_Interface_Field_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         field: MyInterface @semanticNonNull
                                       }

                                       interface MyInterface {
                                         anotherField: String
                                       }

                                       type MyObject implements MyInterface {
                                         anotherField: String
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["field"] = null })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);
        ;

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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
    public async Task SemanticNonNull_Interface_Field_Subgraph_Returns_Error_For_Field_Gateway_Nulls_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyInterface @semanticNonNull @error
            }

            interface MyInterface {
              anotherField: String
            }

            type MyObject implements MyInterface {
              anotherField: String
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          anotherField
                        }
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

    #endregion

    #region Union

    [Fact]
    public async Task Nullable_Union_Field_Subgraph_Returns_Null_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyUnion @null
            }

            union MyUnion = MyObject

            type MyObject {
              anotherField: String
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          __typename
                          ... on MyObject {
                            anotherField
                          }
                      }
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
    public async Task SemanticNonNull_Union_Field_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         field: MyUnion @semanticNonNull
                                       }

                                       union MyUnion = MyObject

                                       type MyObject {
                                         anotherField: String
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["field"] = null })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);
        ;

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          __typename
                          ... on MyObject {
                            anotherField
                          }
                        }
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
    public async Task SemanticNonNull_Union_Field_Subgraph_Returns_Error_For_Field_Gateway_Nulls_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: MyUnion @semanticNonNull @error
            }

            union MyUnion = MyObject

            type MyObject {
              anotherField: String
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(enableSemanticNonNull: true);
        var request = """
                      query {
                        field {
                          __typename
                          ... on MyObject {
                            anotherField
                          }
                        }
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

    #endregion
}
