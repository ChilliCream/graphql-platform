using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class SemanticNonNullTests
{
    [Fact]
    public async Task NonPure_NonNull_Field_Returns_Null_Should_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   nonNullFieldReturningNull
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "TODO",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "nonNullFieldReturningNull"
                  ]
                }
              ],
              "data": {
                "nonNullFieldReturningNull": null
              }
            }
            """);
    }

    [Fact]
    public async Task NonPure_NonNull_Field_Throwing_Should_Null_Field_And_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   nonNullFieldThrowingError
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
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
                    "nonNullFieldThrowingError"
                  ]
                }
              ],
              "data": {
                "nonNullFieldThrowingError": null
              }
            }
            """);
    }

    [Fact]
    public async Task NonPure_Nullable_Field_Returns_Null_Should_Null_Field_Without_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   nullableFieldReturningNull
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "nullableFieldReturningNull": null
              }
            }
            """);
    }

    [Fact]
    public async Task NonPure_NonNull_Object_Field_Returns_Null_Should_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   nonNullObjectFieldReturningNull {
                                     property
                                   }
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "TODO",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "nonNullObjectFieldReturningNull"
                  ]
                }
              ],
              "data": {
                "nonNullObjectFieldReturningNull": null
              }
            }
            """);
    }

    [Fact]
    public async Task NonPure_NonNull_Object_Field_Throwing_Should_Null_Field_And_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   nonNullObjectFieldThrowingError {
                                     property
                                   }
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
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
                    "nonNullObjectFieldThrowingError"
                  ]
                }
              ],
              "data": {
                "nonNullObjectFieldThrowingError": null
              }
            }
            """);
    }

    [Fact]
    public async Task NonPure_Nullable_Object_Field_Returns_Null_Should_Null_Field_Without_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   nullableObjectFieldReturningNull {
                                     property
                                   }
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "nullableObjectFieldReturningNull": null
              }
            }
            """);
    }

    [Fact]
    public async Task Pure_NonNull_Field_Returns_Null_Should_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   pureNonNullFieldReturningNull
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "TODO",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "pureNonNullFieldReturningNull"
                  ]
                }
              ],
              "data": {
                "pureNonNullFieldReturningNull": null
              }
            }
            """);
    }

    [Fact]
    public async Task Pure_NonNull_Field_Throwing_Should_Null_Field_And_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   pureNonNullFieldThrowingError
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
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
                    "pureNonNullFieldThrowingError"
                  ]
                }
              ],
              "data": {
                "pureNonNullFieldThrowingError": null
              }
            }
            """);
    }

    [Fact]
    public async Task Pure_Nullable_Field_Returns_Null_Should_Null_Field_Without_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   pureNullableFieldReturningNull
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "pureNullableFieldReturningNull": null
              }
            }
            """);
    }

    [Fact]
    public async Task Pure_NonNull_Object_Field_Returns_Null_Should_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   pureNonNullObjectFieldReturningNull {
                                     property
                                   }
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "TODO",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "pureNonNullObjectFieldReturningNull"
                  ]
                }
              ],
              "data": {
                "pureNonNullObjectFieldReturningNull": null
              }
            }
            """);
    }

    [Fact]
    public async Task Pure_NonNull_Object_Field_Throwing_Should_Null_Field_And_Produce_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   pureNonNullObjectFieldThrowingError {
                                     property
                                   }
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
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
                    "pureNonNullObjectFieldThrowingError"
                  ]
                }
              ],
              "data": {
                "pureNonNullObjectFieldThrowingError": null
              }
            }
            """);
    }

    [Fact]
    public async Task Pure_Nullable_Object_Field_Returns_Null_Should_Null_Field_Without_Error()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<Query>()
            .ExecuteRequestAsync("""
                                 {
                                   pureNullableObjectFieldReturningNull {
                                     property
                                   }
                                 }
                                 """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "pureNullableObjectFieldReturningNull": null
              }
            }
            """);
    }

    public class Query
    {
        public Task<string> GetNonNullFieldReturningNull()
        {
            return Task.FromResult<string>(null!);
        }

        public Task<string> GetNonNullFieldThrowingError()
        {
            throw new Exception("Something went wrong");
        }

        public Task<string?> GetNullableFieldReturningNull()
        {
            return Task.FromResult<string?>(null);
        }

        public Task<SomeObject> GetNonNullObjectFieldReturningNull()
        {
            return Task.FromResult<SomeObject>(null!);
        }

        public Task<SomeObject> GetNonNullObjectFieldThrowingError()
        {
            throw new Exception("Something went wrong");
        }

        public Task<SomeObject?> GetNullableObjectFieldReturningNull()
        {
            return Task.FromResult<SomeObject?>(null);
        }

        public string PureNonNullFieldReturningNull => null!;

        public string PureNonNullFieldThrowingError => throw new Exception("Somethin went wrong");

        public string? PureNullableFieldReturningNull => null;

        public SomeObject PureNonNullObjectFieldReturningNull => null!;

        public SomeObject PureNonNullObjectFieldThrowingError => throw new Exception("Somethin went wrong");

        public SomeObject? PureNullableObjectFieldReturningNull => null;
    }

    public record SomeObject(string Property);
}
