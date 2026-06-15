using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Verifies typed satellite queue configuration: verbatim names, disable flags,
/// and AutoProvision inheritance and override mechanics.
/// </summary>
public class RabbitMQSatelliteTests
{
    [Fact]
    public void Describe_Should_RenameErrorSatellite_When_ErrorQueueNamedWithPascalCase()
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
    public void Describe_Should_OmitErrorSatellite_When_ErrorDisabled()
    {
        // arrange
        // DisableErrorQueue removes the error satellite from topology entirely; no entity with
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
    public void Describe_Should_OmitSkippedSatellite_When_SkippedDisabled()
    {
        // arrange
        // DisableSkippedQueue removes the skipped satellite from topology entirely; no entity
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
    public void Describe_Should_InheritSatelliteAutoProvision_When_ParentDeclared()
    {
        // arrange
        // parentFalse: transport default true, parent queue declared false; satellites inherit false.
        // The test mirrors the parallel DeclareQueue test and verifies the satellite-config path.
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
    public void Describe_Should_OverrideSatelliteAutoProvision_When_SatelliteSetExplicitly()
    {
        // arrange
        // The parent queue declares AutoProvision(false). A custom convention explicitly sets the
        // error satellite's AutoProvision to true. The convention runs after the default, which used
        // ??= to set the inherited value, so the explicit set wins for the error satellite.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(true);
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoProvision(false);
                t.Queue("orders").Consumer<OrderSpyConsumer>();
                t.AddConvention(new ErrorSatelliteAutoProvisionOverrideConvention(autoProvision: true));
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        // Error satellite: true (explicitly overridden). Main queue and skipped satellite: false
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

    /// <summary>
    /// A convention that explicitly sets the error satellite's AutoProvision value,
    /// overriding the value inherited from the parent queue by the default convention.
    /// </summary>
    private sealed class ErrorSatelliteAutoProvisionOverrideConvention : IRabbitMQReceiveEndpointConfigurationConvention
    {
        private readonly bool _autoProvision;

        public ErrorSatelliteAutoProvisionOverrideConvention(bool autoProvision)
        {
            _autoProvision = autoProvision;
        }

        public void Configure(
            IMessagingConfigurationContext context,
            RabbitMQMessagingTransport transport,
            RabbitMQReceiveEndpointConfiguration configuration)
        {
            configuration.ErrorQueue.AutoProvision = _autoProvision;
        }
    }
}
