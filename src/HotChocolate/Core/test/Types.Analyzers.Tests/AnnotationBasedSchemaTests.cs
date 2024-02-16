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
            .AddCustomModule()
            .UseRequest<SomeRequestMiddleware>();
        
        var result = await services.ExecuteRequestAsync("{ foo }");

        IRequestExecutorBuilder s = default!;
        
        s.AddGraphQL()
            .AddCustomModule()
            .UseRequest<SomeRequestMiddleware>();

        result.MatchSnapshot();
    }
}
