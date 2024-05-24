using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Resolvers;

public class ResolverServiceTests
{
    [Fact]
    public async Task Resolver_Service_Attribute_Default_Scope()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ sayHelloAttribute }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolver_Service_Attribute_Default_Request_Scope()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .ModifyOptions(o => o.DefaultQueryDependencyInjectionScope = DependencyInjectionScope.Request)
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ sayHelloAttribute }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolver_Service_Inferred_Default_Scope()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ sayHelloInferred }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolver_Service_Inferred_Scope_Overriden_On_Resolver()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ sayHelloRequest }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolver_Service_Attribute_Copy_State()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .AddScopedServiceInitializer<SayHelloService>(
                    (request, resolver) =>
                    {
                        resolver.Scope += $"_{request.Scope}";
                    })
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ sayHelloAttribute }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Mutation_Resolver_Service_Attribute_Default_Scope()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .AddMutationType<MutationService>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("mutation { doSomethingAttribute }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Mutation_Resolver_Service_Attribute_Default_Resolver_Scope()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .AddMutationType<MutationService>()
                .ModifyOptions(o => o.DefaultMutationDependencyInjectionScope = DependencyInjectionScope.Resolver)
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("mutation { doSomethingAttribute }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Mutation_Resolver_Service_Inferred_Default_Scope()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .AddMutationType<MutationService>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("mutation { doSomethingInferred }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Mutation_Resolver_Service_Inferred_Scope_Overriden_On_Resolver()
    {
        // arrange
        var services =
            new ServiceCollection()
                .AddScoped<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryService>()
                .AddMutationType<MutationService>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        IExecutionResult result;
        using (var requestScope = services.CreateScope())
        {
            requestScope.ServiceProvider.GetRequiredService<SayHelloService>().Scope = "Request";

            result = await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("mutation { doSomethingResolver }")
                    .SetServices(requestScope.ServiceProvider)
                    .Build());
        }

        result.MatchMarkdownSnapshot();
    }

#if NET8_0_OR_GREATER
    [Fact]
    public async Task Resolver_KeyedService()
    {
        var executor =
            await new ServiceCollection()
                .AddKeyedSingleton("abc", (_, _) => new KeyedService("abc"))
                .AddKeyedSingleton("def", (_, _) => new KeyedService("def"))
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync("{ foo }");

        result.MatchMarkdownSnapshot();
    }
#endif

    public sealed class SayHelloService
    {
        public string Scope = "Resolver";

        public string SayHello() => $"Hello {Scope}";
    }

    public class QueryService
    {
        public string SayHelloAttribute([Service] SayHelloService service)
            => service.SayHello();

        public string SayHelloInferred(SayHelloService service)
            => service.SayHello();

        [UseRequestScope]
        public string SayHelloRequest(SayHelloService service)
            => service.SayHello();
    }

    public class MutationService
    {
        public string DoSomethingAttribute([Service] SayHelloService service)
            => service.SayHello();

        public string DoSomethingInferred(SayHelloService service)
            => service.SayHello();

        [UseResolverScope]
        public string DoSomethingResolver(SayHelloService service)
            => service.SayHello();
    }

    public class SayHelloServicePool : ObjectPool<SayHelloService>
    {
        public bool GetService { get; private set; }

        public bool ReturnService { get; private set; }

        public override SayHelloService Get()
        {
            GetService = true;
            return new SayHelloService();
        }

        public override void Return(SayHelloService obj)
        {
            ReturnService = true;
        }
    }

#if NET8_0_OR_GREATER
    public class Query
    {
        public string Foo([AbcService] KeyedService service)
            => service.Key;
    }

    public class KeyedService(string key)
    {
        public string Key => key;
    }

    public class AbcService() : ServiceAttribute("abc");
#endif
}
