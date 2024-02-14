using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using CookieCrumble;

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
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddCustomModule()
                .ExecuteRequestAsync("{ foo }");

        result.MatchSnapshot();
    }
}
