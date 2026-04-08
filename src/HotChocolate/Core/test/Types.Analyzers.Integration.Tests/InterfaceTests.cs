using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class InterfaceTests
{
    [Fact]
    public async Task Ensure_Interface_Resolvers_Are_ParallelExecutable()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddIntegrationTestTypes()
                .AddPagingArguments()
                .BuildSchemaAsync();

        Assert.True(
            schema.Types.GetType<ObjectType>("Book")
                .Fields["kind"]
                .IsParallelExecutable,
            "Interface resolvers should be parallel executable");
    }
}
