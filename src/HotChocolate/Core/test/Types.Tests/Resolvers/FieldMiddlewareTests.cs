using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class FieldMiddlewareTests
    {
        [Fact]
        public async Task TaskMiddlewareAreCorrectlyConverted()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolve("bar")
                    .Use<TaskFieldMiddleware>())
                .ExecuteRequestAsync("{ foo }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task ValueTaskMiddlewareAreCorrectlyConverted()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolve("bar")
                    .Use<ValueTaskFieldMiddleware>())
                .ExecuteRequestAsync("{ foo }")
                .MatchSnapshotAsync();
        }

        public class TaskFieldMiddleware
        {
            public Task InvokeAsync(IMiddlewareContext context)
            {
                context.Result = "worked";
                return Task.CompletedTask;
                ;
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
}
