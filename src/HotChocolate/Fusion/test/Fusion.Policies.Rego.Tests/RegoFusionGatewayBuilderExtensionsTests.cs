using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

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

    [Fact]
    public async Task AddRegoDataProvider_Should_ResolveContainerOwnedProvider_When_RegisteredByType()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new TestGatewayBuilder("test", services);
        builder.AddRegoPolicies().AddRegoDataProvider<DelegatingTestProvider>();

        await using var rootProvider = services.BuildServiceProvider();
        var setup = rootProvider.GetRequiredService<IOptionsMonitor<FusionGatewaySetup>>().Get("test");

        var schemaServices = new ServiceCollection();
        schemaServices.AddSingleton<IFusionExecutionDiagnosticEvents>(new TestDiagnosticEvents());

        foreach (var modifier in setup.SchemaServiceModifiers)
        {
            modifier(rootProvider, schemaServices);
        }

        var schemaProvider = schemaServices.BuildServiceProvider();

        // act
        var policyProvider = schemaProvider.GetRequiredService<IPolicyProvider>();
        var registeredProvider = schemaProvider.GetRequiredService<DelegatingTestProvider>();
        await WaitUntilAsync(() => registeredProvider.CallCount > 0);

        // assert
        await ((IAsyncDisposable)policyProvider).DisposeAsync();
        Assert.False(registeredProvider.Disposed);
        await schemaProvider.DisposeAsync();
        Assert.True(registeredProvider.Disposed);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("The condition was not met in time.");
    }

    private sealed class TestGatewayBuilder(string name, IServiceCollection services)
        : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }

    private sealed class DelegatingTestProvider : IRegoDataProvider, IDisposable
    {
        private readonly InMemoryRegoDataProvider _inner = new("""{"orders":{}}""");

        public bool Disposed { get; private set; }

        public int CallCount => _inner.CallCount;

        public ValueTask<RegoDataSnapshot> GetDataAsync(CancellationToken cancellationToken)
            => _inner.GetDataAsync(cancellationToken);

        public IChangeToken GetChangeToken() => _inner.GetChangeToken();

        public void Dispose() => Disposed = true;
    }
}
