using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class NonNullListElementTests
{
    [Fact]
    public async Task Execute_Should_ReturnNullElement_When_ElementTypeIsNullable()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { nullableElements { a } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "nullableElements": [
                    {
                      "a": "a"
                    },
                    null
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_SetListToNull_When_NonNullElementIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { nonNullElements { a } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "foo",
                    "nonNullElements",
                    1
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": {
                "foo": {
                  "nonNullElements": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_SetParentToNull_When_NonNullElementOfNonNullListIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { nonNullListOfNonNullElements { a } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "foo",
                    "nonNullListOfNonNullElements",
                    1
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": {
                "foo": null
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_SetListToNull_When_NonNullLeafElementIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { nonNullStrings } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "foo",
                    "nonNullStrings",
                    1
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": {
                "foo": {
                  "nonNullStrings": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_SetOuterListToNull_When_NonNullElementOfNestedListIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { nestedNonNullStrings } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "foo",
                    "nestedNonNullStrings",
                    0,
                    1
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": {
                "foo": {
                  "nestedNonNullStrings": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_SetListToNull_When_NonNullLeafElementCannotBeCoerced()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { nonNullInts } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Int cannot coerce the runtime value of type `System.String` into the result value format.",
                  "path": [
                    "foo",
                    "nonNullInts",
                    1
                  ],
                  "extensions": {
                    "code": "EXEC_INVALID_LEAF_VALUE"
                  }
                }
              ],
              "data": {
                "foo": {
                  "nonNullInts": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_KeepNullElementAndReportError_When_ErrorHandlingModeIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo { nonNullElements { a } } }")
                .SetErrorHandlingMode(ErrorHandlingMode.Null)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "foo",
                    "nonNullElements",
                    1
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": {
                "foo": {
                  "nonNullElements": [
                    {
                      "a": "a"
                    },
                    null
                  ]
                }
              }
            }
            """);
    }

    private const string SchemaText =
        """
        type Query {
            foo: Foo
        }

        type Foo {
            nullableElements: [Bar]
            nonNullElements: [Bar!]
            nonNullListOfNonNullElements: [Bar!]!
            nonNullStrings: [String!]
            nestedNonNullStrings: [[String!]!]
            nonNullInts: [Int!]
        }

        type Bar {
            a: String
        }
        """;

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(SchemaText)
            .ModifyRequestOptions(o => o.AllowErrorHandlingModeOverride = true)
            .AddResolver("Query", "foo", _ => new(new object()))
            .AddResolver("Foo", "nullableElements", _ => new(new[] { new object(), null }))
            .AddResolver("Foo", "nonNullElements", _ => new(new[] { new object(), null }))
            .AddResolver("Foo", "nonNullListOfNonNullElements", _ => new(new[] { new object(), null }))
            .AddResolver("Foo", "nonNullStrings", _ => new(new[] { "a", null }))
            .AddResolver("Foo", "nestedNonNullStrings", _ => new(new[] { new[] { "a", null } }))
            .AddResolver("Foo", "nonNullInts", _ => new(new object[] { 1, "b" }))
            .AddResolver("Bar", "a", _ => new("a"))
            .BuildRequestExecutorAsync();
    }
}
