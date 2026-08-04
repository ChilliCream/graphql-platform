using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.SnapshotHelpers;

namespace HotChocolate.Execution.Errors;

public class ErrorHandlerTests
{
    [Fact]
    public async Task AddFuncErrorFilter()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // error filter configuration
            .AddErrorFilter(error => error.WithCode("Foo123"))

            // general graphql configuration
            .AddGraphQL()
            .AddDocumentFromString("type Query { foo: String }")
            .UseField(_ => _ => throw new Exception("Foo"))

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task FilterOnlyNullRefExceptions()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // general graphql configuration
            .AddGraphQL()
            .AddDocumentFromString("type Query { foo: String bar: String }")
            .AddResolver("Query", "foo", _ => throw new Exception("Foo"))
            .AddResolver("Query", "bar", _ => throw new NullReferenceException("Foo"))

            // error filter configuration
            .AddErrorFilter(
                error =>
                {
                    if (error.Exception is NullReferenceException)
                    {
                        return error.WithCode("NullRef");
                    }
                    return error;
                })

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ foo bar }",
            TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task AddClassErrorFilter()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // error filter configuration
            .AddErrorFilter<DummyErrorFilter>()

            // general graphql configuration
            .AddGraphQL()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver("Query", "foo", _ => throw new Exception("Foo"))
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = false)

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task AddClassErrorFilter_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // general graphql configuration
            .AddGraphQL()
            .AddQueryType<Query>()

            // error filter configuration
            .AddErrorFilter<DummyErrorFilter>()

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task AddClassErrorFilterUsingDI_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // service configuration
            .AddSingleton<SomeService>()

            // general graphql configuration
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddApplicationService<SomeService>()

            // error filter configuration
            .AddErrorFilter<DummyErrorFilterWithDependency>()

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task AddClassErrorFilterUsingFactory_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // general graphql configuration
            .AddGraphQL()
            .AddQueryType<Query>()

            // error filter configuration
            .AddErrorFilter(_ => new DummyErrorFilter())

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task AddClassErrorFilterWithFactory()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // error filter configuration
            .AddErrorFilter(_ => new DummyErrorFilter())

            // general graphql configuration
            .AddGraphQL()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver("Query", "foo", _ => throw new Exception("Foo"))

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task UseAggregateError_In_ErrorFilter()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // error filter configuration
            .AddErrorFilter(_ => new AggregateErrorFilter())

            // general graphql configuration
            .AddGraphQL()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver("Query", "foo", _ => throw new Exception("Foo"))

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task ReportAggregateError_In_Resolver()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            // general graphql configuration
            .AddGraphQL()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver(
                "Query",
                "foo",
                ctx =>
                {
                    ctx.ReportError(
                        new AggregateError(
                            new Error { Message = "abc" },
                            new Error { Message = "def" }));
                    return "Hello";
                })

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task ErrorFilter_Should_BeApplied_When_PureSubFieldResolverThrows()
    {
        // arrange
        var executor = await new ServiceCollection()
            // general graphql configuration
            .AddGraphQL()
            .AddQueryType<BookQuery>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = false)

            // error filter configuration
            .AddErrorFilter(
                error => error.Exception is null ? error : error.WithCode("EXPECTED"))

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              book { id }
              books { id author }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "book"
                  ],
                  "extensions": {
                    "code": "EXPECTED"
                  }
                },
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "books",
                    0,
                    "author"
                  ],
                  "extensions": {
                    "code": "EXPECTED"
                  }
                }
              ],
              "data": {
                "book": null,
                "books": [
                  {
                    "id": "1",
                    "author": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ErrorFilter_Should_TransformNonNullSubFieldError_When_ErrorHandlingModeIsNull()
    {
        // arrange
        var executor = await new ServiceCollection()
            // general graphql configuration
            .AddGraphQL()
            .AddQueryType<BookStoreQuery>()
            .ModifyRequestOptions(
                o =>
                {
                    o.DefaultErrorHandlingMode = ErrorHandlingMode.Null;
                    o.IncludeExceptionDetails = false;
                })

            // error filter configuration
            .AddErrorFilter(
                error =>
                {
                    if (error.Exception is null)
                    {
                        return error;
                    }

                    return error
                        .WithMessage(
                            $"Unexpected Execution Error: {error.Exception.GetType().FullName}")
                        .SetExtension("stackTrace", error.Exception.ToString());
                })

            // build graphql executor
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              book(id: "test") { id }
              books { id author }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Collection(
            result.ExpectOperationResult().Errors!,
            error =>
            {
                Assert.Equal("Unexpected Execution Error: System.ArgumentException", error.Message);
                Assert.Contains("GetBook", (string)error.Extensions!["stackTrace"]!);
            },
            error =>
            {
                Assert.Equal("Unexpected Execution Error: System.ArgumentException", error.Message);
                Assert.Contains("get_Author", (string)error.Extensions!["stackTrace"]!);
            });
    }

    public class DummyErrorFilter : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return error.WithCode("Foo123");
        }
    }

#pragma warning disable CS9113 // Parameter is unread.
    public class DummyErrorFilterWithDependency(SomeService service) : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return error.WithCode("Foo123");
        }
    }
#pragma warning restore CS9113 // Parameter is unread.

    public class SomeService;

    public class AggregateErrorFilter : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return new AggregateError(
                error.WithCode("A"),
                error.WithCode("B"));
        }
    }

    public class Query
    {
        public string GetFoo() => throw new Exception("FooError");
    }

    public class BookQuery
    {
        public Book? GetBook() => throw new ArgumentException("BookError");

        public List<Book> GetBooks() => [new Book()];
    }

    public class Book
    {
        public string Id => "1";

        public string? Author => throw new ArgumentException("AuthorError");
    }

    public class BookStoreQuery
    {
        public StoreBook? GetBook(string id)
            => throw new ArgumentException($"Unknown book '{id}'!");

        public IEnumerable<StoreBook> GetBooks() => [new StoreBook("GraphQL: The Super Guide")];
    }

    public record StoreBook(string Id)
    {
        public string Title => $"{Id}";

        public string Author => throw new ArgumentException($"Missing author for book '{Id}'!");
    }
}
