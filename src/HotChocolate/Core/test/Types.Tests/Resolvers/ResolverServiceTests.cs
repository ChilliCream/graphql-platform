using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Snapshooter.Xunit;

namespace HotChocolate.Resolvers;

public class ResolverServiceTests
{
    [Fact]
    public async Task AddDefaultService()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddSingleton<SayHelloService>()
            .AddGraphQL()
            .AddQueryType<QueryService>()
            .ExecuteRequestAsync("{ sayHello }")
            .MatchSnapshotAsync();
    }

#if NET8_0_OR_GREATER
    [Fact]
    public async Task Resolve_KeyedService()
    {
        Snapshot.FullName();

        var executor =
            await new ServiceCollection()
                .AddKeyedSingleton("abc", (_, __) => new KeyedService("abc"))
                .AddKeyedSingleton("def", (_, __) => new KeyedService("def"))
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        await executor.ExecuteAsync("{ foo }").MatchSnapshotAsync();
    }
#endif
    
    public class SayHelloService
    {
        public string SayHello() => "Hello";
    }

    public class QueryService
    {
        public string SayHello([Service] SayHelloService service)
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
    
    public class AbcService : ServiceAttribute
    {
        public AbcService() : base("abc")
        {
        }
    }
#endif
}
