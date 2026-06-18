using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Verifies concrete error and skipped queue configuration: verbatim names, disable flags,
/// and AutoProvision inheritance and override mechanics.
/// </summary>
public class RabbitMQFaultAndSkippedQueueTests
{
    [Fact]
    public void Describe_Should_RenameErrorQueue_When_ErrorQueueNamedWithPascalCase()
    {
        // arrange
        // The verbatim name "LEGACY.Orders.Error" must survive unchanged; the naming convention
        // must not kebab-case or otherwise transform it.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>()
                    .ErrorQueue("LEGACY.Orders.Error");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OmitErrorQueue_When_ErrorDisabled()
    {
        // arrange
        // DisableErrorQueue removes the error queue from topology entirely; no entity with
        // the conventional "_error" suffix should appear.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>()
                    .DisableErrorQueue();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OmitSkippedQueue_When_SkippedDisabled()
    {
        // arrange
        // DisableSkippedQueue removes the skipped queue from topology entirely; no entity
        // with the conventional "_skipped" suffix should appear.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>()
                    .DisableSkippedQueue();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_InheritFaultAndSkippedQueueAutoProvision_When_ParentDeclared()
    {
        // arrange
        // parentFalse: transport default true, parent queue declared false; error and skipped queues inherit false.
        // The test verifies convention-created fault/skipped queue topology.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(true);
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoProvision(false);
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OverrideErrorQueueAutoProvision_When_ErrorQueueDeclared()
    {
        // arrange
        // The parent queue declares AutoProvision(false). The error queue is explicitly declared
        // with AutoProvision(true), so the explicit topology value wins.
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
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        // Error queue: true (explicitly declared). Main queue and skipped queue: false
        // (inherited from parent).
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        return builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
