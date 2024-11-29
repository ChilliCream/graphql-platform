using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.SnapshotHelpers;

namespace HotChocolate.Execution.Errors;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
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
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

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
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo bar }");

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

            // build graphql executor
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

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
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

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

            // error filter configuration
            .AddErrorFilter<DummyErrorFilterWithDependency>()

            // build graphql executor
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

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
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

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
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

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
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

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
                    ctx.ReportError(new AggregateError(new Error("abc"), new Error("def")));
                    return "Hello";
                })

            // build graphql executor
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        snapshot.Add(result);
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
}
