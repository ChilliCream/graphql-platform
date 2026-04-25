using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Resources.Tests;

public class MochaResourcesServiceCollectionExtensionsTests
{
    [Fact]
    public void GetService_Should_ReturnComposite_When_AddMochaResourcesCalled()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMochaResources();
        var provider = services.BuildServiceProvider();

        // act
        var resolved = provider.GetService<MochaResourceSource>();

        // assert
        Assert.IsType<CompositeMochaResourceSource>(resolved);
    }

    [Fact]
    public void GetService_Should_AggregateContributors_When_MultipleSourcesRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMochaResources();
        services.AddMochaResourceSource(new StaticMochaResourceSource([new TestResource("a", "id-a")]));
        services.AddMochaResourceSource(new StaticMochaResourceSource([new TestResource("b", "id-b")]));
        var provider = services.BuildServiceProvider();

        // act
        var resolved = provider.GetRequiredService<MochaResourceSource>();
        var ids = resolved.Resources.Select(r => r.Id).ToArray();

        // assert
        Assert.Equal(2, resolved.Resources.Count);
        Assert.Contains("id-a", ids);
        Assert.Contains("id-b", ids);
    }

    [Fact]
    public void GetService_Should_ResolveMessageBusContributor_When_AddMochaMessageBusResourcesCalled()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddInMemory();
        services.AddMochaMessageBusResources();
        var provider = services.BuildServiceProvider();

        // act
        var resolved = provider.GetRequiredService<MochaResourceSource>();

        // assert
        Assert.Contains(resolved.Resources, r => r.Kind == "mocha.service");
    }

    [Fact]
    public void GetService_Should_ResolveDefinitionCatalog_When_AddMochaMessageBusResourcesCalled()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddInMemory();
        services.AddMochaMessageBusResources();
        var provider = services.BuildServiceProvider();

        // act
        var catalog = provider.GetRequiredService<IMochaResourceDefinitionCatalog>();

        // assert
        Assert.True(catalog.TryGet("mocha.queue", out var queueDef));
        Assert.Equal("Queue", queueDef.DisplayName);
    }
}
