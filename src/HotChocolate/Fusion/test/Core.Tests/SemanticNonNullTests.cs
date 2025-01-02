using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion;

// TODO: Test with list with more than 3 levels
// TODO: What is supposed to happen with a non-null that bubbles up to a semantic non-null field?
public class SemanticNonNullTests(ITestOutputHelper output)
{
    # region Scalar

    [Fact]
    public async Task Scalar_Field_Nullable_Subgraph_Returns_Null_Gateway_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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
    public async Task Scalar_Field_SemanticNonNull_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
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

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
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
                                         "message": "Cannot return null for semantic non-null field.",
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
                                           "code": "HC0088"
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
    public async Task Scalar_Field_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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
    public async Task Enum_Field_Nullable_Subgraph_Returns_Null_Gateway_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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
    public async Task Enum_Field_SemanticNonNull_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
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

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
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
                                         "message": "Cannot return null for semantic non-null field.",
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
                                           "code": "HC0088"
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
    public async Task Enum_Field_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Only_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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

    #region Composite

    [Fact]
    public async Task Composite_Field_Nullable_Subgraph_Returns_Null_Gateway_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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
    public async Task Composite_Field_SemanticNonNull_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
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

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
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
                                         "message": "Cannot return null for semantic non-null field.",
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
                                           "code": "HC0088"
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
    public async Task Composite_Field_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Only_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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

    #region Composite Subfield

    [Fact]
    public async Task Composite_Field_SubField_Nullable_Subgraph_Returns_Null_Gateway_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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
    public async Task Composite_Field_SubField_SemanticNonNull_Subgraph_Returns_Null_Gateway_Nulls_And_Errors_Field()
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
                            ["field"] = new Dictionary<string, object?> { ["anotherField"] = null }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
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
                                         "message": "Cannot return null for semantic non-null field.",
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
                                           "code": "HC0088"
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
        Composite_Field_SubField_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Only_Nulls_Field()
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
        var executor = await subgraphs.GetExecutorAsync();
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

    #region List

    [Fact]
    public async Task List_Nullable_Subgraph_Returns_Null_For_List_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [String] @null
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "fields": null
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_SemanticNonNull_Subgraph_Returns_Null_For_List_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [String] @semanticNonNull
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["fields"] = null })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields"
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": null
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_SemanticNonNull_Subgraph_Returns_Error_For_List_Gateway_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [String] @semanticNonNull @error
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
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
                                           "fields"
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": null
                                     }
                                   }
                                   """);
    }

    #endregion

    #region Scalar List Item

    [Fact]
    public async Task Scalar_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [String] @null(atIndex: 1)
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "fields": [
                                         "string",
                                         null,
                                         "string"
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        Scalar_ListItem_SemanticNonNull_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_And_Errors_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [String] @semanticNonNull(levels: [1])
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["fields"] = new[] { "a", null, "c" } })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         "a",
                                         null,
                                         "c"
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task Scalar_ListItem_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Only_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [String] @semanticNonNull(levels: [1])
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1]).Build())
                        .SetData(new Dictionary<string, object?> { ["fields"] = new[] { "a", null, "c" } })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         "a",
                                         null,
                                         "c"
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region Enum List Item

    [Fact]
    public async Task Enum_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [MyEnum] @null(atIndex: 1)
            }

            enum MyEnum {
              VALUE
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "fields": [
                                         "VALUE",
                                         null,
                                         "VALUE"
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        Enum_ListItem_SemanticNonNull_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_And_Errors_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [MyEnum] @semanticNonNull(levels: [1])
                                       }

                                       enum MyEnum {
                                         VALUE
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["fields"] = new[] { "VALUE", null, "VALUE" } })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         "VALUE",
                                         null,
                                         "VALUE"
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task Enum_ListItem_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Only_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [MyEnum] @semanticNonNull(levels: [1])
                                       }

                                       enum MyEnum {
                                         VALUE
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1]).Build())
                        .SetData(new Dictionary<string, object?> { ["fields"] = new[] { "VALUE", null, "VALUE" } })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         "VALUE",
                                         null,
                                         "VALUE"
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region Composite List Item

    [Fact]
    public async Task Composite_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [MyObject] @null(atIndex: 1)
            }

            type MyObject {
              anotherField: String
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                       "fields": [
                                         {
                                           "anotherField": "string"
                                         },
                                         null,
                                         {
                                           "anotherField": "string"
                                         }
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        Composite_ListItem_SemanticNonNull_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_And_Errors_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [MyObject] @semanticNonNull(levels: [1])
                                       }

                                       type MyObject {
                                         anotherField: String
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new Dictionary<string, object?> { ["anotherField"] = "1" }, null,
                                new Dictionary<string, object?> { ["anotherField"] = "2" },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         {
                                           "anotherField": "1"
                                         },
                                         null,
                                         {
                                           "anotherField": "2"
                                         }
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task Composite_ListItem_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Only_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [MyObject] @semanticNonNull(levels: [1])
                                       }

                                       type MyObject {
                                         anotherField: String
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1]).Build())
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new Dictionary<string, object?> { ["anotherField"] = "1" }, null,
                                new Dictionary<string, object?> { ["anotherField"] = "2" },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         {
                                           "anotherField": "1"
                                         },
                                         null,
                                         {
                                           "anotherField": "2"
                                         }
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region Composite List Item Subfield

    [Fact]
    public async Task Composite_ListItem_SubField_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [MyObject]
            }

            type MyObject {
              anotherField: String @null(atIndex: 1)
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                       "fields": [
                                         {
                                           "anotherField": "string"
                                         },
                                         {
                                           "anotherField": null
                                         },
                                         {
                                           "anotherField": "string"
                                         }
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        Composite_ListItem_SubField_SemanticNonNull_Subgraph_Returns_Null_For_Field_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [MyObject]
                                       }

                                       type MyObject {
                                         anotherField: String @semanticNonNull
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new Dictionary<string, object?> { ["anotherField"] = "1" },
                                new Dictionary<string, object?> { ["anotherField"] = null },
                                new Dictionary<string, object?> { ["anotherField"] = "2" },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 3,
                                             "column": 5
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           "anotherField"
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         {
                                           "anotherField": "1"
                                         },
                                         {
                                           "anotherField": null
                                         },
                                         {
                                           "anotherField": "2"
                                         }
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        Composite_ListItem_SubField_SemanticNonNull_Subgraph_Returns_Error_For_Field_Gateway_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [MyObject]
                                       }

                                       type MyObject {
                                         anotherField: String @semanticNonNull
                                       }
                                       """)
                .UseField(_ => _ => default)
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1, "anotherField"])
                            .Build())
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new Dictionary<string, object?> { ["anotherField"] = "1" },
                                new Dictionary<string, object?> { ["anotherField"] = null },
                                new Dictionary<string, object?> { ["anotherField"] = "2" },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 3,
                                             "column": 5
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           "anotherField"
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         {
                                           "anotherField": "1"
                                         },
                                         {
                                           "anotherField": null
                                         },
                                         {
                                           "anotherField": "2"
                                         }
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region List of List

    [Fact]
    public async Task List_Of_List_Nullable_Subgraph_Returns_Null_For_List_Gateway_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [[String]] @null
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "fields": null
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_SemanticNonNull_Subgraph_Returns_Null_For_List_Gateway_Nulls_And_Errors_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[String]] @semanticNonNull
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["fields"] = null })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields"
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": null
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_SemanticNonNull_Subgraph_Returns_Error_For_List_Gateway_Only_Nulls_Field()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              fields: [[String]] @semanticNonNull @error
            }
            """,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
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
                                           "fields"
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": null
                                     }
                                   }
                                   """);
    }

    #endregion

    #region List of List Inner List

    [Fact]
    public async Task List_Of_List_Inner_List_Nullable_Subgraph_Returns_Null_For_Inner_List_Gateway_Nulls_Inner_List()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[String]]
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "a" }, null, new[] { "b" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "fields": [
                                         [
                                           "a"
                                         ],
                                         null,
                                         [
                                           "b"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        List_Of_List_Inner_List_SemanticNonNull_Subgraph_Returns_Null_For_Inner_List_Gateway_Nulls_And_Errors_Inner_List()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[String]] @semanticNonNull(levels: [1])
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "a" }, null, new[] { "b" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           "a"
                                         ],
                                         null,
                                         [
                                           "b"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task
        List_Of_List_Inner_List_SemanticNonNull_Subgraph_Returns_Error_For_Inner_List_Gateway_Only_Nulls_Inner_List()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[String]] @semanticNonNull(levels: [1])
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1]).Build())
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "a" }, null, new[] { "b" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           "a"
                                         ],
                                         null,
                                         [
                                           "b"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region List of List Scalar List Item

    [Fact]
    public async Task List_Of_List_Scalar_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[String]]
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "a" }, new string?[] { null }, new[] { "b" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "fields": [
                                         [
                                           "a"
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           "b"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Scalar_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_And_Errors_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[String]] @semanticNonNull(levels: [2])
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "a" }, new string?[] { null }, new[] { "b" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           "a"
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           "b"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Scalar_ListItem_Nullable_Subgraph_Returns_Error_For_ListItem_Gateway_Only_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[String]] @semanticNonNull(levels: [2])
                                       }
                                       """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1, 0]).Build())
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "a" }, new string?[] { null }, new[] { "b" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           "a"
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           "b"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region List of List Enum List Item

    [Fact]
    public async Task List_Of_List_Enum_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyEnum]]
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
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "VALUE" }, new string?[] { null }, new[] { "VALUE" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "fields": [
                                         [
                                           "VALUE"
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           "VALUE"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Enum_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_And_Errors_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyEnum]] @semanticNonNull(levels: [2])
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
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "VALUE" }, new string?[] { null }, new[] { "VALUE" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           "VALUE"
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           "VALUE"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Enum_ListItem_Nullable_Subgraph_Returns_Error_For_ListItem_Gateway_Only_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyEnum]] @semanticNonNull(levels: [2])
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
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1, 0]).Build())
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[] { new[] { "VALUE" }, new string?[] { null }, new[] { "VALUE" } }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           "VALUE"
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           "VALUE"
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region List of List Composite List Item

    [Fact]
    public async Task List_Of_List_Composite_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyObject]]
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
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "a"
                                    }
                                },
                                new Dictionary<string, object?>?[] {
                                    null
                                },
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "b"
                                    }
                                },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                       "fields": [
                                         [
                                           {
                                             "anotherField": "a"
                                           }
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           {
                                             "anotherField": "b"
                                           }
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Composite_ListItem_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_And_Errors_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyObject]] @semanticNonNull(levels: [2])
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
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "a"
                                    }
                                },
                                new Dictionary<string, object?>?[] {
                                    null
                                },
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "b"
                                    }
                                },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           {
                                             "anotherField": "a"
                                           }
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           {
                                             "anotherField": "b"
                                           }
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Composite_ListItem_Nullable_Subgraph_Returns_Error_For_ListItem_Gateway_Only_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyObject]] @semanticNonNull(levels: [2])
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
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1, 0]).Build())
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "a"
                                    }
                                },
                                new Dictionary<string, object?>?[] {
                                    null
                                },
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "b"
                                    }
                                },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           {
                                             "anotherField": "a"
                                           }
                                         ],
                                         [
                                           null
                                         ],
                                         [
                                           {
                                             "anotherField": "b"
                                           }
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion

    #region List of List Composite List Item Subfield

    [Fact]
    public async Task List_Of_List_Composite_ListItem_SubField_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyObject]]
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
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "a"
                                    }
                                },
                                new Dictionary<string, object?>?[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = null
                                    }
                                },
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "b"
                                    }
                                },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                       "fields": [
                                         [
                                           {
                                             "anotherField": "a"
                                           }
                                         ],
                                         [
                                           {
                                             "anotherField": null
                                           }
                                         ],
                                         [
                                           {
                                             "anotherField": "b"
                                           }
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Composite_ListItem_SubField_Nullable_Subgraph_Returns_Null_For_ListItem_Gateway_Nulls_And_Errors_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyObject]]
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
                            ["fields"] = new[]
                            {
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "a"
                                    }
                                },
                                new Dictionary<string, object?>?[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = null
                                    }
                                },
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "b"
                                    }
                                },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Cannot return null for semantic non-null field.",
                                         "locations": [
                                           {
                                             "line": 3,
                                             "column": 5
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0,
                                           "anotherField"
                                         ],
                                         "extensions": {
                                           "code": "HC0088"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           {
                                             "anotherField": "a"
                                           }
                                         ],
                                         [
                                           {
                                             "anotherField": null
                                           }
                                         ],
                                         [
                                           {
                                             "anotherField": "b"
                                           }
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task List_Of_List_Composite_ListItem_SubField_Nullable_Subgraph_Returns_Error_For_ListItem_Gateway_Only_Nulls_ListItem()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            configure: builder => builder
                .AddDocumentFromString("""
                                       type Query {
                                         fields: [[MyObject]]
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
                        .AddError(ErrorBuilder.New().SetMessage("Some error").SetPath(["fields", 1, 0, "anotherField"]).Build())
                        .SetData(new Dictionary<string, object?>
                        {
                            ["fields"] = new[]
                            {
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "a"
                                    }
                                },
                                new Dictionary<string, object?>?[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = null
                                    }
                                },
                                new[] {
                                    new Dictionary<string, object?>
                                    {
                                        ["anotherField"] = "b"
                                    }
                                },
                            }
                        })
                        .Build();
                    return default;
                })
            ,
            enableSemanticNonNull: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        fields {
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
                                         "message": "Some error",
                                         "locations": [
                                           {
                                             "line": 3,
                                             "column": 5
                                           }
                                         ],
                                         "path": [
                                           "fields",
                                           1,
                                           0,
                                           "anotherField"
                                         ]
                                       }
                                     ],
                                     "data": {
                                       "fields": [
                                         [
                                           {
                                             "anotherField": "a"
                                           }
                                         ],
                                         [
                                           {
                                             "anotherField": null
                                           }
                                         ],
                                         [
                                           {
                                             "anotherField": "b"
                                           }
                                         ]
                                       ]
                                     }
                                   }
                                   """);
    }

    #endregion
}
