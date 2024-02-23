using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using CookieCrumble;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Types;

public class SchemaTests
{
    [Fact]
    public async Task SchemaSnapshot()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddCustomModule()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }
    
    [Fact]
    public async Task ExecuteRootField()
    {
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddCustomModule();
        
        var result = await services.ExecuteRequestAsync("{ foo }");

        result.MatchSnapshot();
    }
    
    [Fact]
    public async Task ExecuteWithMiddleware()
    {
        var services = new ServiceCollection()
            .AddSingleton<Service1>()
            .AddSingleton<Service2>()
            .AddGraphQL()
            .AddCustomModule()
            .UseRequest<SomeRequestMiddleware>()
            .UseDefaultPipeline();
        
        var result = await services.ExecuteRequestAsync("{ foo }");

        result.MatchSnapshot();
    }
}
