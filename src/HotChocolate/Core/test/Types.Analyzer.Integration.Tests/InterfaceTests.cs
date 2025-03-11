using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class InterfaceTests
{
    [Fact]
    public async Task Schema_Snapshot()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Interface_Resolvers_Are_ParallelExecutable()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddIntegrationTestTypes()
                .BuildSchemaAsync();

        Assert.True(
            schema.GetType<ObjectType>("Book")
                .Fields["kind"]
                .IsParallelExecutable,
            "Interface resolvers should be parallel executable");
    }
}
