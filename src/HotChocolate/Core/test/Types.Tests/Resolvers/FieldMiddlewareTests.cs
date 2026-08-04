using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Tests;

namespace HotChocolate.Resolvers;

public class FieldMiddlewareTests
{
    [Fact]
    public async Task TaskMiddlewareAreCorrectlyConverted()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar")
                .Use<TaskFieldMiddleware>())
            .ExecuteRequestAsync(
                "{ foo }",
                cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ValueTaskMiddlewareAreCorrectlyConverted()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar")
                .Use<ValueTaskFieldMiddleware>())
            .ExecuteRequestAsync(
                "{ foo }",
                cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    public class TaskFieldMiddleware
    {
        public Task InvokeAsync(IMiddlewareContext context)
        {
            context.Result = "worked";
            return Task.CompletedTask;
        }
    }

    public class ValueTaskFieldMiddleware
    {
        public ValueTask InvokeAsync(IMiddlewareContext context)
        {
            context.Result = "worked";
            return default;
        }
    }
}
