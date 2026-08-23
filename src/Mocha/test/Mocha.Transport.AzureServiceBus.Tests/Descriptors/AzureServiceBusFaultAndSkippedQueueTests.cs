using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.TestHelpers;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;
using Mocha.Transport.AzureServiceBus.Tests.Topology;

namespace Mocha.Transport.AzureServiceBus.Tests.Descriptors;

/// <summary>
/// Verifies concrete fault and skipped queue configuration: custom addresses, disable flags, and
/// AutoProvision inheritance and override mechanics, mirroring the ownership rules already covered
/// for RabbitMQ error and skipped queues.
/// </summary>
public class AzureServiceBusFaultAndSkippedQueueTests
{
    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    [Fact]
    public void Describe_Should_UseCustomFaultQueue_When_FaultEndpointAddressConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>()
                    .FaultEndpoint(new Uri("queue:legacy-orders-error"));
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - the custom address is used verbatim instead of the conventional "_error" suffix.
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OmitFaultQueue_When_FaultEndpointDisabled()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>()
                    .DisableFaultEndpoint();
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - no entity with the conventional "_error" suffix appears.
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OmitSkippedQueue_When_SkippedEndpointDisabled()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>()
                    .DisableSkippedEndpoint();
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - no entity with the conventional "_skipped" suffix appears.
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_InheritFaultAndSkippedAutoProvision_When_ParentQueueDeclared()
    {
        // arrange
        // The parent queue is declared with AutoProvision(false); the fault and skipped queues are
        // fabricated by convention and inherit that same value rather than the transport default.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(true);
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoProvision(false);
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OverrideFaultAutoProvision_When_FaultQueueDeclaredExplicitly()
    {
        // arrange
        // The parent queue declares AutoProvision(false), but the fault queue is separately declared
        // with AutoProvision(true), so the explicit topology value wins for that entity alone.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(true);
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoProvision(false);
                t.DeclareQueue("orders_error").AutoProvision(true);
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - fault queue: true (explicitly declared). Main and skipped queues: false (inherited).
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

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
