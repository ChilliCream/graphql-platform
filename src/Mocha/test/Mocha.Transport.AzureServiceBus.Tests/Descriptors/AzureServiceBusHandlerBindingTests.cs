using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.TestHelpers;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;
using Mocha.Transport.AzureServiceBus.Tests.Topology;

namespace Mocha.Transport.AzureServiceBus.Tests.Descriptors;

/// <summary>
/// Covers how a registered handler is claimed into a convention-named receive endpoint, and how
/// that claim's convention topology renders under implicit and explicit bind modes.
/// </summary>
public class AzureServiceBusHandlerBindingTests
{
    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    [Fact]
    public void BindImplicitly_Should_AutoDiscoverHandler_When_HandlerRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // assert
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    [Fact]
    public void BindImplicitly_Should_DescribeConventionTopology_When_HandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - the convention topic and the subscription into the handler's auto-named queue appear.
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void BindExplicitly_Should_ThrowOnBuild_When_HandlerNotManuallyBound()
    {
        // arrange & act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindExplicitly()));

        // assert
        Assert.Contains(nameof(OrderCreatedHandler), exception.Message);
    }

    [Fact]
    public void BindExplicitly_Should_DescribeViaUnifiedQueue_When_HandlerAttachedToQueue()
    {
        // arrange
        // A handler placed on the unified Queue() API constitutes an explicit claim; the convention
        // topic and subscription are suppressed and only the declared queue remains.
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders-handler").Handler<OrderCreatedHandler>();
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IAzureServiceBusMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        return builder
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configureTransport(t);
            })
            .BuildRuntime();
    }
}
