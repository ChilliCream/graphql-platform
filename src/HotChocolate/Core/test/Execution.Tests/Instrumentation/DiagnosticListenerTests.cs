using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.StarWars;
using HotChocolate.StarWars.Models;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Instrumentation;

public class DiagnosticListenerTests
{
    [Fact]
    public async Task Intercept_Resolver_Result_With_Listener()
    {
        // arrange
        var listener = new TestListener();
        var executor = await CreateExecutorAsync(c => c
            .AddDiagnosticEventListener(_ => listener)
            .AddStarWarsTypes()
            .Services
            .AddStarWarsRepositories());

        // act
        var result = await executor.ExecuteAsync("{ hero { name } }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        Assert.Collection(listener.Results, r => Assert.IsType<Droid>(r));
    }

    [Fact]
    public async Task Intercept_Resolver_Result_With_Listener_2()
    {
        // arrange
        var services = new ServiceCollection()
            .AddSingleton<Touched>()
            .AddGraphQL()
            .AddDiagnosticEventListener<TouchedListener>()
            .AddStarWars()
            .Services
            .BuildServiceProvider();

        // act
        await services.ExecuteRequestAsync("{ hero { name } }");

        // assert
        Assert.True(services.GetRequiredService<Touched>().Signal);
    }

    [Fact]
    public async Task Intercept_Resolver_Result_With_Multiple_Listener()
    {
        // arrange
        var listenerA = new TestListener();
        var listenerB = new TestListener();
        var executor = await CreateExecutorAsync(c => c
            .AddDiagnosticEventListener(_ => listenerA)
            .AddDiagnosticEventListener(_ => listenerB)
            .AddStarWarsTypes()
            .Services
            .AddStarWarsRepositories());

        // act
        var result = await executor.ExecuteAsync("{ hero { name } }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        Assert.Collection(listenerA.Results, r => Assert.IsType<Droid>(r));
        Assert.Collection(listenerB.Results, r => Assert.IsType<Droid>(r));
    }

    public class Touched
    {
        public bool Signal = false;
    }

    private class TouchedListener : ExecutionDiagnosticEventListener
    {
        private readonly Touched _touched;

        public TouchedListener(Touched touched)
        {
            _touched = touched;
        }

        public override IDisposable ExecuteRequest(IRequestContext context)
        {
            _touched.Signal = true;
            return EmptyScope;
        }
    }

    private sealed class TestListener : ExecutionDiagnosticEventListener
    {
        public List<object?> Results { get; } = [];

        public override bool EnableResolveFieldValue => true;

        public override IDisposable ResolveFieldValue(IMiddlewareContext context)
        {
            return new ResolverActivityScope(context, Results);
        }

        private sealed class ResolverActivityScope : IDisposable
        {
            public ResolverActivityScope(IMiddlewareContext context, List<object?> results)
            {
                Context = context;
                Results = results;
            }

            private IMiddlewareContext Context { get; }

            public List<object?> Results { get; }

            public void Dispose()
            {
                Results.Add(Context.Result);
            }
        }
    }
}
