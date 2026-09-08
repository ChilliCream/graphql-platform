using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoFusionGatewayBuilderExtensionsTests
{
    [Fact]
    public async Task AddRegoDataProvider_Should_ThrowConfigurationError_When_NameIsDuplicatedAtBuild()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new TestGatewayBuilder("test", services);
        builder.AddRegoPolicies()
            .AddRegoDataProvider("dup", new InMemoryRegoDataProvider("{}"))
            .AddRegoDataProvider("dup", new InMemoryRegoDataProvider("{}"));

        await using var rootProvider = services.BuildServiceProvider();
        var setup = rootProvider.GetRequiredService<IOptionsMonitor<FusionGatewaySetup>>().Get("test");

        var schemaServices = new ServiceCollection();
        schemaServices.AddSingleton<IFusionExecutionDiagnosticEvents>(new TestDiagnosticEvents());

        foreach (var modifier in setup.SchemaServiceModifiers)
        {
            modifier(rootProvider, schemaServices);
        }

        await using var schemaProvider = schemaServices.BuildServiceProvider();

        // act
        void Act() => schemaProvider.GetRequiredService<IPolicyProvider>();

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public async Task AddRegoDataProvider_Should_RegisterProvider_When_NamesAreUnique()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new TestGatewayBuilder("test", services);
        builder.AddRegoPolicies()
            .AddRegoDataProvider("orders", new InMemoryRegoDataProvider("""{"orders":{}}"""));

        await using var rootProvider = services.BuildServiceProvider();
        var setup = rootProvider.GetRequiredService<IOptionsMonitor<FusionGatewaySetup>>().Get("test");

        var schemaServices = new ServiceCollection();
        schemaServices.AddSingleton<IFusionExecutionDiagnosticEvents>(new TestDiagnosticEvents());

        foreach (var modifier in setup.SchemaServiceModifiers)
        {
            modifier(rootProvider, schemaServices);
        }

        await using var schemaProvider = schemaServices.BuildServiceProvider();

        // act
        var policyProvider = schemaProvider.GetRequiredService<IPolicyProvider>();

        // assert
        Assert.NotNull(policyProvider);
        await ((IAsyncDisposable)policyProvider).DisposeAsync();
    }

    private sealed class TestGatewayBuilder(string name, IServiceCollection services)
        : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
}
