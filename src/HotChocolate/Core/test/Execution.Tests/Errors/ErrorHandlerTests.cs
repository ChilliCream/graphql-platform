using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using Snapshooter.Xunit;

namespace HotChocolate.Execution.Errors;

public class ErrorHandlerTests
{
    [Fact]
    public async Task AddFuncErrorFilter()
    {
        Snapshot.FullName();
        await ExpectError(
            "{ foo }",
            b => b
                .AddDocumentFromString("type Query { foo: String }")
                .UseField(_ => _ => throw new Exception("Foo"))
                .Services
                .AddErrorFilter(error => error.WithCode("Foo123")));
    }

    [Fact]
    public async Task FilterOnlyNullRefExceptions()
    {
        Snapshot.FullName();
        await ExpectError(
            "{ foo bar }",
            b => b
                .AddDocumentFromString("type Query { foo: String bar: String }")
                .AddResolver("Query", "foo", _ => throw new Exception("Foo"))
                .AddResolver("Query", "bar", _ => throw new NullReferenceException("Foo"))
                .AddErrorFilter(
                    error =>
                    {
                        if (error.Exception is NullReferenceException)
                        {
                            return error.WithCode("NullRef");
                        }
                        return error;
                    }),
            expectedErrorCount: 2);
    }

    [Fact]
    public async Task AddClassErrorFilter()
    {
        Snapshot.FullName();
        await ExpectError(
            "{ foo }",
            b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", _ => throw new Exception("Foo"))
                .Services
                .AddErrorFilter<DummyErrorFilter>());
    }

    [Fact]
    public async Task AddClassErrorFilter_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        var schema = await serviceCollection
            .AddGraphQLServer()
            .AddErrorFilter<DummyErrorFilter>()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var resp = await schema.ExecuteAsync("{ foo }");

        // assert
        resp.MatchSnapshot();
    }

    [Fact]
    public async Task AddClassErrorFilterUsingDI_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<SomeService>();
        var schema = await serviceCollection
            .AddGraphQLServer()
            .AddErrorFilter<DummyErrorFilterWithDependency>()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var resp = await schema.ExecuteAsync("{ foo }");

        // assert
        resp.MatchSnapshot();
    }

    [Fact]
    public async Task
        AddClassErrorFilterUsingFactory_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        var schema = await serviceCollection
            .AddGraphQLServer()
            .AddErrorFilter(f => new DummyErrorFilter())
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var resp = await schema.ExecuteAsync("{ foo }");

        // assert
        resp.MatchSnapshot();
    }

    [Fact]
    public async Task AddClassErrorFilterWithFactory()
    {
        Snapshot.FullName();
        await ExpectError(
            "{ foo }",
            b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", _ => throw new Exception("Foo"))
                .Services
                .AddErrorFilter(_ => new DummyErrorFilter()));
    }

    [Fact]
    public async Task UseAggregateError_In_ErrorFilter()
    {
        Snapshot.FullName();

        await ExpectError(
            "{ foo }",
            b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", _ => throw new Exception("Foo"))
                .Services
                .AddErrorFilter(_ => new AggregateErrorFilter()));
    }

    [Fact]
    public async Task ReportAggregateError_In_Resolver()
    {
        Snapshot.FullName();

        await ExpectError(
            "{ foo }",
            b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver(
                    "Query",
                    "foo",
                    ctx =>
                    {
                        ctx.ReportError(new AggregateError(new Error("abc"), new Error("def")));
                        return "Hello";
                    }),
            expectedErrorCount: 2);
    }

    private async Task ExpectError(
        string query,
        Action<IRequestExecutorBuilder> configure,
        int expectedErrorCount = 1)
    {
        var errors = 0;

        await TestHelper.ExpectError(
            query,
            b =>
            {
                configure(b);
                b.AddErrorFilter(
                    error =>
                    {
                        errors++;
                        return error;
                    });
            });

        Assert.Equal(expectedErrorCount, errors);
    }

    public class DummyErrorFilter : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return error.WithCode("Foo123");
        }
    }

    public class DummyErrorFilterWithDependency : IErrorFilter
    {
        private readonly SomeService _service;

        public DummyErrorFilterWithDependency(SomeService service)
        {
            _service = service;
        }

        public IError OnError(IError error)
        {
            return error.WithCode("Foo123");
        }
    }

    public class SomeService { }

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
