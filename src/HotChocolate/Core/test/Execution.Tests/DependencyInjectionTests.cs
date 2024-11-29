using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DependencyInjectionTests
{
    [Fact]
    public async Task Extension_With_Constructor_Injection()
    {
        // this test ensures that we inject services into type instances without the need of
        // registering the type into the dependency container.
        var executor =
            await new ServiceCollection()
                .AddSingleton<SomeService>()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddType<ExtendQuery1>()
                .BuildRequestExecutorAsync();

        new
        {
            result1 = await executor
                .ExecuteAsync("{ hello }")
                .ToJsonAsync(),
            result2 = await executor
                .ExecuteAsync("{ hello }")
                .ToJsonAsync(),
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Extension_With_Scoped_Constructor_Injection()
    {
        IServiceProvider services =
            new ServiceCollection()
                .AddScoped<SomeService>()
                .AddScoped<ExtendQuery1>()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddType<ExtendQuery1>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        var result = new string[2];

        using (var scope = services.CreateScope())
        {
            result[0] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build())
                .ToJsonAsync();
        }

        using (var scope = services.CreateScope())
        {
            result[1] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build())
                .ToJsonAsync();
        }

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Type_With_Constructor_Injection()
    {
        // this test ensures that we inject services into type instances without the need of
        // registering the type into the dependency container.
        var executor =
            await new ServiceCollection()
                .AddSingleton<SomeService>()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .BuildRequestExecutorAsync();

        new
        {
            result1 = await executor
                .ExecuteAsync("{ hello }")
                .ToJsonAsync(),
            result2 = await executor
                .ExecuteAsync("{ hello }")
                .ToJsonAsync(),
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Type_With_Scoped_Constructor_Injection()
    {
        IServiceProvider services =
            new ServiceCollection()
                .AddScoped<SomeService>()
                .AddScoped<Query2>()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        var result = new string[2];

        using (var scope = services.CreateScope())
        {
            result[0] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build())
                .ToJsonAsync();
        }

        using (var scope = services.CreateScope())
        {
            result[1] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build())
                .ToJsonAsync();
        }

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Keyed_Services_Do_Not_Throw()
    {
        var services =
            new ServiceCollection()
                .AddKeyedScoped<Query1>("abc")
                .AddScoped<SomeService>()
                .AddScoped<Query2>()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        using var scope = services.CreateScope();

        await executor
            .ExecuteAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ hello }")
                    .SetServices(scope.ServiceProvider)
                    .Build())
            .ToJsonAsync()
            .MatchSnapshotAsync();
    }

    public class SomeService
    {
        private int _i;

        public int Count => _i;

        public string SayHello() => "Hello_" + _i++;
    }

    public class Query1
    {
    }

    [ExtendObjectType(typeof(Query1))]
    public class ExtendQuery1
    {
        private readonly SomeService _service;

        public ExtendQuery1(SomeService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public string Hello() => _service.SayHello();
    }

    public class Query2
    {
        private readonly SomeService _service;

        public Query2(SomeService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public string Hello() => _service.SayHello();
    }
}
