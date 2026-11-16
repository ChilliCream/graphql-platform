using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.SnapshotHelpers;

namespace HotChocolate.Execution.Errors;

public class ErrorBehaviorTests
{
    [Fact]
    public async Task MatchTestSchema()
    {
        var executor = await CreateExecutorAsync();
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task SyntaxError()
        => await MatchQueryAsync("{ error1");

    [Fact]
    public async Task AsyncMethod_NoAwait_Throw_ApplicationError()
        => await MatchQueryAsync("{ error1 }");

    [Fact]
    public async Task AsyncMethod_Await_Throw_ApplicationError()
        => await MatchQueryAsync("{ error4 }");

    [Fact]
    public async Task SyncMethod_Throw_ApplicationError()
        => await MatchQueryAsync("{ error7 }");

    [Fact]
    public async Task Property_Throw_ApplicationError()
        => await MatchQueryAsync("{ error10 }");

    [Fact]
    public async Task AsyncMethod_NoAwait_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error2 }");

    [Fact]
    public async Task AsyncMethod_Await_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error5 }");

    [Fact]
    public async Task SyncMethod_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error8 }");

    [Fact]
    public async Task Property_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error11 }");

    [Fact]
    public async Task AsyncMethod_NoAwait_Return_ApplicationError()
        => await MatchQueryAsync("{ error3 }");

    [Fact]
    public async Task AsyncMethod_Await_Return_ApplicationError()
        => await MatchQueryAsync("{ error6 }");

    [Fact]
    public async Task SyncMethod_Return_ApplicationError()
        => await MatchQueryAsync("{ error9 }");

    [Fact]
    public async Task Property_Return_ApplicationError()
        => await MatchQueryAsync("{ error12 }");

    [Fact]
    public async Task Property_Return_UnexpectedErrorWithPath()
        => await MatchQueryAsync("{ error13 }");

    [Fact]
    public async Task Error_On_NonNull_Root_Field()
        => await MatchQueryAsync("{ error15 }");

    [Fact]
    public Task RootFieldNotDefined()
        => MatchQueryAsync("query { doesNotExist }");

    [Fact]
    public async Task RootTypeNotDefined()
        => await MatchQueryAsync("mutation { doesNotExist }");

    [Fact]
    public async Task Error_Filter_Adds_Details()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddErrorFilter(error => error.SetExtension("foo", "bar"))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ error14 }",
            TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task Resolver_InvalidParentCast()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("foo")
                .Type<ObjectType<Foo>>()
                .Extend()
                // in the pure resolver we will return the wrong type
                .Configuration.Resolver = _ => new ValueTask<object?>(new Baz()))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { bar } }",
            TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task PureResolver_InvalidParentCast()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("foo")
                .Type<ObjectType<Foo>>()
                .Extend()
                // in the pure resolver we will return the wrong type
                .Configuration.PureResolver = _ => new Baz())
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ foo { bar } }",
            TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async Task GraphQLException_Should_ReportFieldPath_When_CreatedFromMessage()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PathQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // message is a pure resolver and delayedMessage runs as a resolver task.
        var pureResult = await executor.ExecuteAsync(
            "{ parent { children { message } } }",
            TestContext.Current.CancellationToken);
        var asyncResult = await executor.ExecuteAsync(
            "{ parent { children { delayedMessage } } }",
            TestContext.Current.CancellationToken);

        // assert
        new[] { pureResult, asyncResult }.MatchInlineSnapshots(
            [
                """
                {
                  "errors": [
                    {
                      "message": "message error",
                      "path": [
                        "parent",
                        "children",
                        1,
                        "message"
                      ]
                    }
                  ],
                  "data": {
                    "parent": {
                      "children": [
                        {
                          "message": "ok"
                        },
                        {
                          "message": null
                        }
                      ]
                    }
                  }
                }
                """,
                """
                {
                  "errors": [
                    {
                      "message": "message error",
                      "path": [
                        "parent",
                        "children",
                        1,
                        "delayedMessage"
                      ]
                    }
                  ],
                  "data": {
                    "parent": {
                      "children": [
                        {
                          "delayedMessage": "ok"
                        },
                        {
                          "delayedMessage": null
                        }
                      ]
                    }
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task GraphQLException_Should_ReportFieldPath_When_CreatedFromErrorWithoutPath()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PathQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // error is a pure resolver and delayedError runs as a resolver task.
        var pureResult = await executor.ExecuteAsync(
            "{ parent { children { error } } }",
            TestContext.Current.CancellationToken);
        var asyncResult = await executor.ExecuteAsync(
            "{ parent { children { delayedError } } }",
            TestContext.Current.CancellationToken);

        // assert
        new[] { pureResult, asyncResult }.MatchInlineSnapshots(
            [
                """
                {
                  "errors": [
                    {
                      "message": "error builder error",
                      "path": [
                        "parent",
                        "children",
                        1,
                        "error"
                      ]
                    }
                  ],
                  "data": {
                    "parent": {
                      "children": [
                        {
                          "error": "ok"
                        },
                        {
                          "error": null
                        }
                      ]
                    }
                  }
                }
                """,
                """
                {
                  "errors": [
                    {
                      "message": "error builder error",
                      "path": [
                        "parent",
                        "children",
                        1,
                        "delayedError"
                      ]
                    }
                  ],
                  "data": {
                    "parent": {
                      "children": [
                        {
                          "delayedError": "ok"
                        },
                        {
                          "delayedError": null
                        }
                      ]
                    }
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task Resolver_Should_ReturnSameResultAsReportError_When_ExceptionIsRethrown()
    {
        // arrange
        var executor = await CreateReportOrThrowExecutorAsync();

        // act
        var reported = await executor.ExecuteAsync(
            "{ value: reported }",
            TestContext.Current.CancellationToken);
        var rethrown = await executor.ExecuteAsync(
            "{ value: rethrown }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(reported.ToJson(), rethrown.ToJson());
        rethrown.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "value"
                  ],
                  "extensions": {
                    "exception": "resolver failed"
                  }
                }
              ],
              "data": {
                "value": null
              }
            }
            """);
    }

    [Fact]
    public async Task Resolver_Should_ReturnSameResultAsReportError_When_GraphQLExceptionWrapsExceptionError()
    {
        // arrange
        var executor = await CreateReportOrThrowExecutorAsync();

        // act
        var reported = await executor.ExecuteAsync(
            "{ value: reportedWithCode }",
            TestContext.Current.CancellationToken);
        var thrown = await executor.ExecuteAsync(
            "{ value: thrownWithCode }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(reported.ToJson(), thrown.ToJson());
        thrown.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "value"
                  ],
                  "extensions": {
                    "code": "RESOLVER_FAILED",
                    "exception": "resolver failed"
                  }
                }
              ],
              "data": {
                "value": null
              }
            }
            """);
    }

    [Fact]
    public async Task SetMaxAllowedValidationErrors_To_1()
    {
        // arrange
        using var snapshot = StartResultSnapshot();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("foo")
                .Type<ObjectType<Foo>>()
                .Extend()
                // in the pure resolver we will return the wrong type
                .Configuration.PureResolver = _ => new Baz())
            .SetMaxAllowedValidationErrors(1)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ a b c d }",
            TestContext.Current.CancellationToken);

        // assert
        snapshot.Add(result);
    }

    private static async Task MatchQueryAsync(string query)
    {
        using var snapshot = StartResultSnapshot();

        var executor = await CreateExecutorAsync();
        var result = await executor.ExecuteAsync(query);

        snapshot.Add(result);
    }

    private static async Task<IRequestExecutor> CreateReportOrThrowExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<ReportOrThrowQuery>()
            .AddErrorFilter(
                error => error.Exception is null
                    ? error
                    : error.SetExtension("exception", error.Exception.Message))
            .BuildRequestExecutorAsync();
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .BuildRequestExecutorAsync();
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.Error3()).Type<StringType>();
            descriptor.Field(t => t.Error6()).Type<StringType>();
            descriptor.Field(t => t.Error9()).Type<StringType>();
            descriptor.Field(t => t.Error12).Type<StringType>();
        }
    }

    public class Query
    {
        public Task<string?> Error1()
        {
            throw new GraphQLException("query error 1");
        }

        public Task<string?> Error2()
        {
            throw new Exception("query error 2");
        }

        public Task<object?> Error3()
        {
            return Task.FromResult<object?>(
                ErrorBuilder.New()
                    .SetMessage("query error 3")
                    .Build());
        }

        public async Task<string?> Error4()
        {
            await Task.Delay(1);
            throw new GraphQLException("query error 4");
        }

        public async Task<string?> Error5()
        {
            await Task.Delay(1);
            throw new Exception("query error 5");
        }

        public async Task<object?> Error6()
        {
            await Task.Delay(1);
            return await Task.FromResult<object>(
                ErrorBuilder.New()
                    .SetMessage("query error 6")
                    .Build());
        }

        public string? Error7()
        {
            throw new GraphQLException("query error 7");
        }

        public string? Error8()
        {
            throw new Exception("query error 8");
        }

        public object? Error9()
        {
            return ErrorBuilder.New()
                .SetMessage("query error 9")
                .Build();
        }

        public string? Error10 => throw new GraphQLException("query error 10");

        public string? Error11 => throw new Exception("query error 11");

        public object? Error12 => ErrorBuilder.New()
            .SetMessage("query error 12")
            .Build();

        public Foo? Error13 => new Foo();

        public string? Error14 => throw new ArgumentNullException("Error14");

        public string Error15 => throw new ArgumentNullException("Error15");
    }

    public class Foo
    {
        public string? Bar => throw new Exception("baz");
    }

    public class Baz
    {
        public string? Bar => throw new Exception("baz");
    }

    public class PathQuery
    {
        public PathParent Parent => new();
    }

    public class PathParent
    {
        public PathChild[] Children => [new PathChild(fail: false), new PathChild(fail: true)];
    }

    public class PathChild(bool fail)
    {
        public string? Message => fail ? throw new GraphQLException("message error") : "ok";

        public string? Error => fail ? throw CreateErrorBuilderException() : "ok";

        public async Task<string?> DelayedMessageAsync()
        {
            await Task.Delay(1);
            return fail ? throw new GraphQLException("message error") : "ok";
        }

        public async Task<string?> DelayedErrorAsync()
        {
            await Task.Delay(1);
            return fail ? throw CreateErrorBuilderException() : "ok";
        }

        private static GraphQLException CreateErrorBuilderException()
            => new(ErrorBuilder.New().SetMessage("error builder error").Build());
    }

    public class ReportOrThrowQuery
    {
        public string? Reported(IResolverContext context)
        {
            try
            {
                return Fail();
            }
            catch (InvalidOperationException ex)
            {
#pragma warning disable CS0618 // ReportError is obsolete
                context.ReportError(ex);
#pragma warning restore CS0618 // ReportError is obsolete
                return null;
            }
        }

        public string? Rethrown()
        {
            try
            {
                return Fail();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
        }

        public string? ReportedWithCode(IResolverContext context)
        {
            try
            {
                return Fail();
            }
            catch (InvalidOperationException ex)
            {
#pragma warning disable CS0618 // ReportError is obsolete
                context.ReportError(ex, error => error.SetCode("RESOLVER_FAILED"));
#pragma warning restore CS0618 // ReportError is obsolete
                return null;
            }
        }

        public string? ThrownWithCode()
        {
            try
            {
                return Fail();
            }
            catch (InvalidOperationException ex)
            {
                throw new GraphQLException(
                    ErrorBuilder.FromException(ex)
                        .SetCode("RESOLVER_FAILED")
                        .Build());
            }
        }

        private static string Fail() => throw new InvalidOperationException("resolver failed");
    }
}
