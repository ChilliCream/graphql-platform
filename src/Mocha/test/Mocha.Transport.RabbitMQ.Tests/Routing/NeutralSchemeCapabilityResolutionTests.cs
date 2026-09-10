using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Routing;

public sealed class NeutralSchemeCapabilityResolutionTests
{
    [Fact]
    public void Resolve_Should_UseOnlyMatchingTransport_When_NeutralTopicHasSingleCandidate()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddRabbitMQ(t =>
            {
                t.Name("rabbit");
                t.Schema("rabbit");
                t.ConnectionProvider(_ => new StubConnectionProvider());
            })
            .AddInMemory(t =>
            {
                t.Name("memory");
                t.Schema("memory");
            });

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("topic:orders"));

        // assert
        Assert.Equal("memory", endpoint.Transport.Name);
    }

    [Fact]
    public void Resolve_Should_UseOnlyMatchingTransport_When_NeutralExchangeHasSingleCandidate()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddInMemory(t =>
            {
                t.Name("memory");
                t.Schema("memory");
            })
            .AddRabbitMQ(t =>
            {
                t.Name("rabbit");
                t.Schema("rabbit");
                t.ConnectionProvider(_ => new StubConnectionProvider());
            });

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("exchange:orders"));

        // assert
        Assert.Equal("rabbit", endpoint.Transport.Name);
    }
}
