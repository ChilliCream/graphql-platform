using HotChocolate.Adapters.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

public sealed class McpSetupCompositionTests
{
    [Fact]
    public void AddMcp_Should_Compose_When_CalledMultipleTimes()
    {
        // arrange
        var services = new ServiceCollection();
        var optionsCalls = new List<string>();
        var builderCalls = new List<string>();

        // act
        services
            .AddGraphQL()
            .AddMcp(
                options => optionsCalls.Add("first"),
                builder => builderCalls.Add("first"));

        services
            .AddGraphQL()
            .AddMcp(
                options => optionsCalls.Add("second"),
                builder => builderCalls.Add("second"));

        var setup = services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<McpSetup>>()
            .Get(ISchemaDefinition.DefaultName);

        foreach (var modifier in setup.ServerOptionsModifiers)
        {
            modifier(new McpServerOptions());
        }

        foreach (var modifier in setup.ServerModifiers)
        {
            modifier(null!);
        }

        // assert
        Assert.Equal(["first", "second"], optionsCalls);
        Assert.Equal(["first", "second"], builderCalls);
    }

    [Fact]
    public void SchemaNames_Should_ReturnConfiguredNames_When_MultipleSchemasRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQL("alpha").AddMcp();
        services.AddGraphQL("beta").AddMcp();

        // act
        var manager = services.BuildServiceProvider().GetRequiredService<McpManager>();

        // assert
        Assert.Equal(["alpha", "beta"], manager.SchemaNames.Order());
    }
}
