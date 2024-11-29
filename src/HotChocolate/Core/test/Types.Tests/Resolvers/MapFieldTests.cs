using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Resolvers;

public class MapFieldTests
{
    [Fact]
    public async Task MapField_WithDelegate()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("Wrong"))
            .MapField(new FieldReference("Query", "foo"),
                _ => context =>
                {
                    context.Result = "Correct";
                    return default;
                })
            .ExecuteRequestAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MapField_WithClass()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("Wrong"))
            .MapField<Middleware>(new FieldReference("Query", "foo"))
            .ExecuteRequestAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MapField_WithFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Middleware>();

        await services
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("Wrong"))
            .MapField(
                new FieldReference("Query", "foo"),
                (sp, _) => sp.GetRequiredService<Middleware>())
            .ExecuteRequestAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    public class Middleware
    {
        public Task InvokeAsync(IMiddlewareContext context)
        {
            context.Result = "worked";
            return Task.CompletedTask;
        }
    }
}
